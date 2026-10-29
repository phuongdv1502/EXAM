import { Component, ElementRef, HostListener, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, interval, switchMap, takeWhile } from 'rxjs';
import { OmrBatch, OmrResultRow, OmrSheet } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';
import { CameraFacing, cameraDeviceId, openCameraStream } from './omr-camera';
import { OmrScanPanel } from './omr-scan';

@Component({
  selector: 'app-omr-batch',
  imports: [FormsModule, RouterLink, OmrScanPanel, ...UI],
  templateUrl: './omr-batch.html',
})
export class OmrBatchPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private polling = false;

  batch: OmrBatch | null = null;
  sheets: OmrSheet[] = [];
  total = 0;
  results: OmrResultRow[] = [];
  status: string | null = null;
  page = 1;
  pageSize = 20;
  showResults = false;
  cameraOpen = false;
  cameraError = '';
  cameraNote = '';
  facing: CameraFacing = 'environment';
  scanOpen = false;

  @ViewChild('preview') private preview?: ElementRef<HTMLVideoElement>;
  private stream: MediaStream | null = null;

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => {
      this.stopStream();
      if (this.cameraOpen) document.body.style.overflow = '';
    });
    this.refresh();
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    if (this.cameraOpen) this.closeCamera();
  }

  batchId(): number {
    return Number(this.route.snapshot.paramMap.get('id'));
  }

  locked(): boolean {
    return this.batch?.status === 'Approved' || this.batch?.status === 'Published';
  }

  percent(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  refresh(): void {
    const id = this.batchId();
    this.watch(forkJoin({ batch: this.api.omrBatch(id), sheets: this.api.omrSheets(id, this.status, this.page, this.pageSize) }), (data) => {
      this.batch = data.batch;
      this.sheets = data.sheets.items;
      this.total = data.sheets.total;
      this.arm();
    });
  }

  turn(event: { page?: number; rows?: number }): void {
    this.page = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.refresh();
  }

  openScan(): void {
    if (this.locked() || this.scanOpen) return;
    this.closeCamera();
    this.scanOpen = true;
  }

  closeScan(): void {
    this.scanOpen = false;
    this.cdr.markForCheck();
  }

  openCamera(): void {
    if (this.locked() || this.cameraOpen) return;
    this.scanOpen = false;
    this.cameraError = '';
    this.cameraNote = '';
    this.facing = 'environment';
    this.cameraOpen = true;
    document.body.style.overflow = 'hidden';
    this.cdr.detectChanges();
    void this.startCamera();
  }

  closeCamera(): void {
    this.stopStream();
    this.cameraOpen = false;
    this.cameraError = '';
    this.cameraNote = '';
    document.body.style.overflow = '';
    this.cdr.markForCheck();
  }

  shoot(): void {
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth || this.loading) {
      this.cameraError = 'Camera chưa có hình. Đợi một chút rồi chụp lại.';
      this.cdr.markForCheck();
      return;
    }
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext('2d');
    if (!context) return;
    context.drawImage(video, 0, 0);
    canvas.toBlob((blob) => {
      if (!blob) {
        this.cameraError = 'Không tạo được ảnh từ camera.';
        this.cdr.markForCheck();
        return;
      }
      const file = new File([blob], `phieu-camera-${Date.now()}.jpg`, { type: 'image/jpeg' });
      this.closeCamera();
      this.loading = true;
      this.watch(this.api.uploadOmrSheets(this.batchId(), [file], 'Camera'), (result) => {
        const duplicates = result.sheets.filter((item) => item.duplicate).length;
        this.showOk(duplicates ? `Đã nhận phiếu. ${duplicates} trang trùng nên không tạo thêm.` : 'Đã nhận phiếu. Hệ thống đang nhận dạng.');
        this.refresh();
      });
    }, 'image/jpeg', 0.92);
  }

  onFiles(event: Event, sourceType: string): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length || this.locked()) return;
    this.loading = true;
    this.watch(this.api.uploadOmrSheets(this.batchId(), files, sourceType), (result) => {
      const duplicates = result.sheets.filter((item) => item.duplicate).length;
      this.showOk(duplicates ? `Đã nhận phiếu. ${duplicates} trang trùng nên không tạo thêm.` : 'Đã nhận phiếu. Hệ thống đang nhận dạng.');
      this.refresh();
    });
  }

  retry(sheet: OmrSheet): void {
    this.loading = true;
    this.watch(this.api.retryOmrSheet(this.batchId(), sheet.id), () => {
      this.showOk('Đã xếp lại phiếu để nhận dạng.');
      this.refresh();
    });
  }

  grade(): void {
    this.loading = true;
    this.watch(this.api.gradeOmrBatch(this.batchId()), (batch) => {
      this.batch = batch;
      this.showOk('Đã chấm các phiếu đã nhận hoặc đã xác nhận.');
      this.loadResults();
      this.refresh();
    });
  }

  approve(): void {
    this.loading = true;
    this.watch(this.api.approveOmrBatch(this.batchId()), (batch) => {
      this.batch = batch;
      this.showOk('Đã duyệt đợt. Có thể công bố điểm.');
      this.refresh();
    });
  }

  publish(): void {
    this.loading = true;
    this.watch(this.api.publishOmrBatch(this.batchId()), (batch) => {
      this.batch = batch;
      this.showOk('Đã công bố điểm cho học sinh.');
      this.refresh();
    });
  }

  loadResults(): void {
    this.showResults = true;
    this.watch(this.api.omrResults(this.batchId()), (rows) => (this.results = rows));
  }

  exportFile(): void {
    this.watch(this.api.omrExport(this.batchId()), (blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'bang-diem-omr.xlsx';
      link.click();
      URL.revokeObjectURL(url);
      this.showOk('Đã tải bảng điểm.');
    });
  }

  async flipCamera(): Promise<void> {
    const previousId = cameraDeviceId(this.stream);
    this.facing = this.facing === 'environment' ? 'user' : 'environment';
    this.cameraError = '';
    this.cameraNote = '';
    this.stopStream();
    this.cdr.markForCheck();
    await this.startCamera();
    if (this.cameraError) {
      this.facing = this.facing === 'user' ? 'environment' : 'user';
      this.cameraError = '';
      await this.startCamera();
      if (!this.cameraError) this.cameraNote = 'Không mở được camera kia. Vẫn dùng camera đang mở.';
    } else if (previousId && cameraDeviceId(this.stream) === previousId) {
      this.cameraNote = 'Máy này không đổi được camera. Hãy dùng camera đang mở.';
    }
    this.cdr.markForCheck();
  }

  private async startCamera(): Promise<void> {
    if (!navigator.mediaDevices?.getUserMedia) {
      this.cameraError = 'Trình duyệt này không mở được camera. Hãy dùng nút tải ảnh.';
      this.cdr.markForCheck();
      return;
    }
    try {
      this.stream = await openCameraStream(this.facing, 1920, 1080);
    } catch {
      this.cameraError = 'Không mở được camera. Cho phép quyền camera trên trình duyệt, hoặc tải ảnh phiếu lên.';
      this.cdr.markForCheck();
      return;
    }
    if (!this.cameraOpen) {
      this.stopStream();
      return;
    }
    const video = this.preview?.nativeElement;
    if (!video) {
      this.cameraError = 'Không hiện được khung camera.';
      this.stopStream();
      this.cdr.markForCheck();
      return;
    }
    video.srcObject = this.stream;
    try {
      await video.play();
    } catch {
      this.cameraError = 'Camera đã mở nhưng chưa phát được hình.';
    }
    this.cdr.markForCheck();
  }

  private stopStream(): void {
    this.stream?.getTracks().forEach((track) => track.stop());
    this.stream = null;
    if (this.preview?.nativeElement) this.preview.nativeElement.srcObject = null;
  }

  private arm(): void {
    const busy = this.sheets.some((sheet) => sheet.status === 'Uploaded' || sheet.status === 'Processing')
      || this.batch?.status === 'Uploading'
      || this.batch?.status === 'Processing';
    if (!busy || this.polling) {
      if (!busy) this.polling = false;
      return;
    }
    this.polling = true;
    interval(2000)
      .pipe(
        switchMap(() => forkJoin({
          batch: this.api.omrBatch(this.batchId()),
          sheets: this.api.omrSheets(this.batchId(), this.status, this.page, this.pageSize),
        })),
        takeWhile(() => this.polling),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (data) => {
          this.batch = data.batch;
          this.sheets = data.sheets.items;
          this.total = data.sheets.total;
          const still = data.sheets.items.some((sheet) => sheet.status === 'Uploaded' || sheet.status === 'Processing')
            || data.batch.status === 'Uploading'
            || data.batch.status === 'Processing';
          if (!still) this.polling = false;
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.polling = false;
          this.showError(error);
          this.cdr.markForCheck();
        },
      });
  }
}

import { AfterViewInit, Component, ElementRef, HostListener, OnInit, ViewChild, input, output } from '@angular/core';
import { OmrScanResult } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';
import { apiMessage } from '../core/api-error';
import { CameraFacing, cameraDeviceId, cameraFailure, openCameraStream } from './omr-camera';
import { Point, SheetLook, SheetMiss, findSheet, orderCorners, sheetPageHeight, sheetPageWidth, warpSheet } from './omr-sheet-frame';

@Component({
  selector: 'app-omr-scan',
  imports: [...UI],
  templateUrl: './omr-scan.html',
})
export class OmrScanPanel extends Page implements OnInit, AfterViewInit {
  readonly batchId = input<number | null>(null);
  readonly closed = output<void>();
  readonly scanned = output<void>();

  @ViewChild('preview') private preview?: ElementRef<HTMLVideoElement>;

  phase = 'Đang mở camera...';
  cameraError = '';
  facing: CameraFacing = 'environment';
  result: OmrScanResult | null = null;
  guide = '';
  ready = false;
  stageView = '0 0 4 3';
  private stream: MediaStream | null = null;
  private timer = 0;
  private previous: Uint8ClampedArray | null = null;
  private previousCorners: Point[] | null = null;
  private held: Uint8ClampedArray | null = null;
  private stable = 0;
  busy = false;
  private waitUntil = 0;
  private scratch: HTMLCanvasElement | null = null;
  private sheetCanvas: HTMLCanvasElement | null = null;
  private audio: AudioContext | null = null;
  private cameraTries = 0;

  ngOnInit(): void {
    document.body.style.overflow = 'hidden';
    this.armChime();
    this.destroyRef.onDestroy(() => this.stop());
  }

  ngAfterViewInit(): void {
    void this.startCamera();
  }

  @HostListener('document:keydown.escape')
  close(): void {
    this.stop();
    this.closed.emit();
  }

  async flipCamera(): Promise<void> {
    if (this.busy) return;
    const previousId = cameraDeviceId(this.stream);
    this.facing = this.facing === 'environment' ? 'user' : 'environment';
    this.resetLock();
    this.result = null;
    this.cameraError = '';
    window.clearInterval(this.timer);
    this.timer = 0;
    this.stream?.getTracks().forEach((track) => track.stop());
    this.stream = null;
    this.phase = this.facing === 'user' ? 'Đang mở camera trước...' : 'Đang mở camera sau...';
    this.cdr.markForCheck();
    await this.startCamera();
    if (this.cameraError) {
      this.facing = this.facing === 'user' ? 'environment' : 'user';
      this.cameraError = '';
      await this.startCamera();
      if (!this.cameraError) this.phase = 'Không mở được camera kia. Vẫn dùng camera đang mở.';
    } else if (previousId && cameraDeviceId(this.stream) === previousId) {
      this.phase = 'Máy này không đổi được camera. Hãy dùng camera đang mở.';
    }
    this.cdr.markForCheck();
  }

  shoot(): void {
    if (this.busy || this.cameraError) return;
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth) {
      this.phase = 'Camera chưa có hình. Đợi một chút rồi chụp lại.';
      this.cdr.markForCheck();
      return;
    }
    const look = this.inspect(video);
    if (!look.corners) {
      this.phase = 'Chưa thấy đủ phiếu. Đưa cả tờ vào khung rồi chụp.';
      this.cdr.markForCheck();
      return;
    }
    const print = this.fingerprint(video);
    const corners = orderCorners(look.corners, this.previousCorners?.[0] ?? null);
    this.guide = this.trace(video, corners);
    this.previousCorners = corners;
    this.capture(print ?? new Uint8ClampedArray(1), corners);
  }

  private async startCamera(): Promise<void> {
    if (!navigator.mediaDevices?.getUserMedia) {
      this.cameraError = 'Trình duyệt này không mở được camera.';
      this.cdr.markForCheck();
      return;
    }
    const video = this.preview?.nativeElement;
    if (!video) {
      if (this.cameraTries++ < 12) {
        requestAnimationFrame(() => void this.startCamera());
        return;
      }
      this.cameraError = 'Không hiện được khung camera.';
      this.cdr.markForCheck();
      return;
    }
    this.cameraTries = 0;
    try {
      this.stream = await openCameraStream(this.facing, 1920, 1440);
    } catch (error) {
      this.cameraError = cameraFailure(error, 'Không mở được camera. Hãy cho phép quyền camera trên trình duyệt.');
      this.cdr.markForCheck();
      return;
    }
    video.srcObject = this.stream;
    try {
      await video.play();
    } catch {
      this.cameraError = 'Camera đã mở nhưng chưa phát được hình.';
      this.cdr.markForCheck();
      return;
    }
    this.phase = 'Đưa cả phiếu vào khung.';
    this.timer = window.setInterval(() => this.sample(), 200);
    this.cdr.markForCheck();
  }

  private sample(): void {
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth || this.busy) return;
    const print = this.fingerprint(video);
    const look = this.inspect(video);
    if (this.held && print && (this.difference(print, this.held) > 16 || !look.corners)) {
      this.held = null;
      this.stable = 0;
      this.previousCorners = null;
      this.phase = 'Đưa phiếu tiếp theo vào khung.';
      this.previous = print;
      this.cdr.markForCheck();
      return;
    }
    if (this.held) {
      this.previous = print;
      this.phase = 'Đưa phiếu tiếp theo vào khung.';
      this.cdr.markForCheck();
      return;
    }
    if (!look.corners || look.problem) {
      this.guide = look.corners ? this.trace(video, look.corners) : '';
      this.ready = false;
      this.stable = 0;
      this.previousCorners = null;
      this.phase = this.problemText(look.problem ?? 'missing');
      this.previous = print;
      this.cdr.markForCheck();
      return;
    }

    const corners = orderCorners(look.corners, this.previousCorners?.[0] ?? null);
    this.guide = this.trace(video, corners);
    if (look.brightness < 70) {
      this.holdOff(print, corners, 'Chưa đủ sáng.');
      return;
    }
    if (look.sharpness < 6) {
      this.holdOff(print, corners, 'Ảnh chưa nét. Giữ phiếu yên.');
      return;
    }

    const limit = video.videoWidth * 0.02;
    const moved = !this.previousCorners || this.shifted(corners, this.previousCorners, limit);
    this.previousCorners = corners;
    this.previous = print;
    this.stable = moved ? 1 : this.stable + 1;
    this.ready = !moved;
    if (this.stable >= 4 && print && Date.now() >= this.waitUntil) {
      this.capture(print, corners);
      return;
    }
    this.phase = this.ready ? 'Ảnh đạt. Giữ yên để chụp...' : 'Giữ phiếu yên...';
    this.cdr.markForCheck();
  }

  private holdOff(print: Uint8ClampedArray | null, corners: Point[], phase: string): void {
    this.ready = false;
    this.stable = 0;
    this.previousCorners = corners;
    this.previous = print;
    this.phase = phase;
    this.cdr.markForCheck();
  }

  private capture(print: Uint8ClampedArray, corners: Point[]): void {
    this.busy = true;
    this.ready = true;
    this.phase = 'Đang chỉnh phiếu và chấm...';
    this.cdr.markForCheck();
    void this.finishCapture(print, corners);
  }

  private async finishCapture(print: Uint8ClampedArray, corners: Point[]): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    const file = await this.frameFile(corners);
    if (!file) {
      this.busy = false;
      this.phase = 'Không chụp được phiếu.';
      this.waitUntil = Date.now() + 1200;
      this.cdr.markForCheck();
      return;
    }
    this.watch(
      this.api.scanOmr(file, this.batchId()),
      (result) => {
        this.busy = false;
        this.result = result;
        this.phase = result.accepted ? 'Đưa phiếu tiếp theo vào khung.' : result.message;
        if (result.accepted) {
          this.held = print;
          this.chime();
          this.scanned.emit();
        } else {
          this.waitUntil = Date.now() + 1200;
        }
      },
      (error: unknown) => {
        this.busy = false;
        this.phase = apiMessage(error);
        this.waitUntil = Date.now() + 1500;
        this.showError(error);
      },
    );
  }

  private inspect(video: HTMLVideoElement): SheetLook {
    const sampleWidth = 480;
    const sampleHeight = Math.max(1, Math.round((video.videoHeight * sampleWidth) / video.videoWidth));
    this.sheetCanvas ??= document.createElement('canvas');
    this.sheetCanvas.width = sampleWidth;
    this.sheetCanvas.height = sampleHeight;
    const context = this.sheetCanvas.getContext('2d', { willReadFrequently: true });
    if (!context) return { corners: null, problem: 'missing', brightness: 0, sharpness: 0 };
    context.drawImage(video, 0, 0, sampleWidth, sampleHeight);
    const image = context.getImageData(0, 0, sampleWidth, sampleHeight);
    const look = findSheet(image.data, sampleWidth, sampleHeight);
    if (!look.corners) return look;
    return {
      ...look,
      corners: look.corners.map((point) => ({
        x: (point.x * video.videoWidth) / sampleWidth,
        y: (point.y * video.videoHeight) / sampleHeight,
      })),
    };
  }

  private trace(video: HTMLVideoElement, corners: Point[]): string {
    const width = Math.max(1, video.clientWidth);
    const height = Math.max(1, video.clientHeight);
    this.stageView = `0 0 ${width} ${height}`;
    const scale = Math.min(width / video.videoWidth, height / video.videoHeight);
    const left = (width - video.videoWidth * scale) / 2;
    const top = (height - video.videoHeight * scale) / 2;
    return corners.map((point) => `${(left + point.x * scale).toFixed(1)},${(top + point.y * scale).toFixed(1)}`).join(' ');
  }

  private async frameFile(corners: Point[]): Promise<File | null> {
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth) return null;
    const scale = Math.min(1, 2000 / Math.max(video.videoWidth, video.videoHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(video.videoWidth * scale));
    canvas.height = Math.max(1, Math.round(video.videoHeight * scale));
    const context = canvas.getContext('2d', { willReadFrequently: true });
    if (!context) return null;
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    const warped = warpSheet(
      context.getImageData(0, 0, canvas.width, canvas.height),
      corners.map((point) => ({ x: point.x * scale, y: point.y * scale })),
      sheetPageWidth,
      sheetPageHeight,
    );
    if (!warped) return null;
    const output = document.createElement('canvas');
    output.width = sheetPageWidth;
    output.height = sheetPageHeight;
    const target = output.getContext('2d');
    if (!target) return null;
    target.putImageData(warped, 0, 0);
    return new Promise((resolve) => {
      output.toBlob((blob) => {
        resolve(blob ? new File([blob], `quet-${Date.now()}.jpg`, { type: 'image/jpeg' }) : null);
      }, 'image/jpeg', 0.9);
    });
  }

  private fingerprint(video: HTMLVideoElement): Uint8ClampedArray | null {
    const size = 24;
    this.scratch ??= document.createElement('canvas');
    this.scratch.width = size;
    this.scratch.height = size;
    const context = this.scratch.getContext('2d', { willReadFrequently: true });
    if (!context) return null;
    context.drawImage(video, 0, 0, size, size);
    const data = context.getImageData(0, 0, size, size).data;
    const gray = new Uint8ClampedArray(size * size);
    for (let index = 0; index < gray.length; index++) {
      const offset = index * 4;
      gray[index] = (data[offset] * 0.3 + data[offset + 1] * 0.59 + data[offset + 2] * 0.11) | 0;
    }
    return gray;
  }

  private difference(left: Uint8ClampedArray, right: Uint8ClampedArray): number {
    let sum = 0;
    for (let index = 0; index < left.length; index++) sum += Math.abs(left[index] - right[index]);
    return sum / left.length;
  }

  private shifted(next: Point[], previous: Point[], limit: number): boolean {
    return next.some((point, index) => {
      const other = previous[index];
      return !other || Math.hypot(point.x - other.x, point.y - other.y) > limit;
    });
  }

  private problemText(problem: SheetMiss): string {
    switch (problem) {
      case 'far':
        return 'Đưa phiếu lại gần, cho đủ bốn góc vào khung.';
      case 'clipped':
        return 'Lùi phiếu ra để thấy đủ bốn góc.';
      case 'skew':
        return 'Chỉnh phiếu cho bốn góc rõ hơn.';
      default:
        return 'Đưa cả phiếu vào khung.';
    }
  }

  private resetLock(): void {
    this.held = null;
    this.previous = null;
    this.previousCorners = null;
    this.stable = 0;
    this.guide = '';
    this.ready = false;
  }

  private armChime(): void {
    const Ctx = window.AudioContext ?? (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctx) return;
    this.audio ??= new Ctx();
    void this.audio.resume();
  }

  private chime(): void {
    this.armChime();
    const ctx = this.audio;
    if (!ctx) return;
    void ctx.resume().then(() => {
      const now = ctx.currentTime;
      const strike = (frequency: number, start: number, duration: number, peak: number) => {
        const tone = ctx.createOscillator();
        const gain = ctx.createGain();
        tone.type = 'sine';
        tone.frequency.value = frequency;
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.exponentialRampToValueAtTime(peak, start + 0.012);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
        tone.connect(gain);
        gain.connect(ctx.destination);
        tone.start(start);
        tone.stop(start + duration + 0.02);
      };
      strike(2349, now, 1.15, 0.08);
      strike(3520, now, 0.55, 0.045);
      strike(4698, now, 0.28, 0.02);
    }).catch(() => undefined);
  }

  private stop(): void {
    window.clearInterval(this.timer);
    this.timer = 0;
    this.stream?.getTracks().forEach((track) => track.stop());
    this.stream = null;
    if (this.preview?.nativeElement) this.preview.nativeElement.srcObject = null;
    void this.audio?.close();
    this.audio = null;
    document.body.style.overflow = '';
  }
}

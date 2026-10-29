import { Component, ElementRef, HostListener, OnInit, ViewChild, input, output } from '@angular/core';
import { OmrScanResult } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';
import { apiMessage } from '../core/api-error';
import { CameraFacing, cameraDeviceId, openCameraStream } from './omr-camera';

@Component({
  selector: 'app-omr-scan',
  imports: [...UI],
  templateUrl: './omr-scan.html',
})
export class OmrScanPanel extends Page implements OnInit {
  readonly batchId = input<number | null>(null);
  readonly closed = output<void>();
  readonly scanned = output<void>();

  @ViewChild('preview') private preview?: ElementRef<HTMLVideoElement>;

  phase = 'Đang mở camera...';
  cameraError = '';
  facing: CameraFacing = 'environment';
  result: OmrScanResult | null = null;
  private stream: MediaStream | null = null;
  private timer = 0;
  private previous: Uint8ClampedArray | null = null;
  private held: Uint8ClampedArray | null = null;
  private stable = 0;
  private busy = false;
  private waitUntil = 0;
  private scratch: HTMLCanvasElement | null = null;
  private audio: AudioContext | null = null;

  ngOnInit(): void {
    document.body.style.overflow = 'hidden';
    this.armChime();
    this.destroyRef.onDestroy(() => this.stop());
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
    this.held = null;
    this.previous = null;
    this.stable = 0;
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

  private async startCamera(): Promise<void> {
    if (!navigator.mediaDevices?.getUserMedia) {
      this.cameraError = 'Trình duyệt này không mở được camera.';
      this.cdr.markForCheck();
      return;
    }
    try {
      this.stream = await openCameraStream(this.facing, 1600, 1200);
    } catch {
      this.cameraError = 'Không mở được camera. Hãy cho phép quyền camera trên trình duyệt.';
      this.cdr.markForCheck();
      return;
    }
    const video = this.preview?.nativeElement;
    if (!video) {
      this.stop();
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
    this.phase = 'Đưa cả phiếu vào khung, rồi giữ yên.';
    this.timer = window.setInterval(() => this.sample(), 500);
    this.cdr.markForCheck();
  }

  private sample(): void {
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth || this.busy) return;
    const print = this.fingerprint(video);
    if (!print) return;
    const now = Date.now();
    if (this.held) {
      if (this.difference(print, this.held) > 16) {
        this.held = null;
        this.stable = 0;
        this.result = null;
        this.phase = 'Đưa phiếu tiếp theo vào khung.';
      }
      this.previous = print;
      this.cdr.markForCheck();
      return;
    }
    if (now < this.waitUntil) {
      this.previous = print;
      return;
    }
    const mean = print.reduce((sum, value) => sum + value, 0) / print.length;
    if (mean < 28) {
      this.phase = 'Chưa đủ sáng.';
      this.stable = 0;
      this.previous = print;
      this.cdr.markForCheck();
      return;
    }
    const delta = this.previous ? this.difference(print, this.previous) : 99;
    this.previous = print;
    if (delta < 5) this.stable += 1;
    else this.stable = 0;
    this.phase = this.stable === 0 ? 'Đang tìm phiếu...' : 'Giữ phiếu yên...';
    this.cdr.markForCheck();
    if (this.stable >= 2) {
      this.stable = 0;
      void this.capture(print);
    }
  }

  private async capture(print: Uint8ClampedArray): Promise<void> {
    const file = await this.frameFile();
    if (!file) return;
    this.busy = true;
    this.phase = 'Đang nhận mẫu và chấm...';
    this.cdr.markForCheck();
    this.watch(this.api.scanOmr(file, this.batchId()), (result) => {
      this.busy = false;
      this.result = result;
      this.phase = result.message;
      if (result.accepted) {
        this.held = print;
        this.chime();
        this.scanned.emit();
      } else {
        this.waitUntil = Date.now() + 1200;
      }
    }, (error: unknown) => {
      this.busy = false;
      this.phase = apiMessage(error);
      this.waitUntil = Date.now() + 1500;
      this.showError(error);
    });
  }

  private frameFile(): Promise<File | null> {
    const video = this.preview?.nativeElement;
    if (!video?.videoWidth) return Promise.resolve(null);
    const scale = Math.min(1, 1400 / Math.max(video.videoWidth, video.videoHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(video.videoWidth * scale));
    canvas.height = Math.max(1, Math.round(video.videoHeight * scale));
    const context = canvas.getContext('2d');
    if (!context) return Promise.resolve(null);
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    return new Promise((resolve) => {
      canvas.toBlob((blob) => {
        resolve(blob ? new File([blob], `quet-${Date.now()}.jpg`, { type: 'image/jpeg' }) : null);
      }, 'image/jpeg', 0.82);
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

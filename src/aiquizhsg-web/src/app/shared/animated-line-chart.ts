import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  ViewChild,
  inject,
} from '@angular/core';

interface PlotPoint {
  x: number;
  y: number;
  label: string;
  value: number;
}

interface AxisTick {
  y: number;
  label: string;
}

@Component({
  selector: 'app-animated-line-chart',
  templateUrl: './animated-line-chart.html',
})
export class AnimatedLineChart implements OnChanges, AfterViewInit, OnDestroy {
  private readonly cdr = inject(ChangeDetectorRef);
  private static nextId = 0;

  @Input() title = 'Student Performance';
  @Input() subtitle = 'Theo dõi kết quả học tập qua các lần kiểm tra.';
  @Input() badge = 'Analytics';
  @Input() labels: string[] = [];
  @Input() series: number[] = [];
  @Input() yAxisMin: number | null = null;
  @Input() yAxisMax: number | null = null;
  @Input() unit = '%';

  @ViewChild('trend') trend?: ElementRef<SVGPathElement>;

  readonly fillId = `perf-fill-${AnimatedLineChart.nextId++}`;
  readonly width = 720;
  readonly height = 280;
  readonly pad = { l: 48, r: 20, t: 20, b: 44 };

  points: PlotPoint[] = [];
  ticks: AxisTick[] = [];
  line = '';
  area = '';
  length = 0;
  offset = 0;
  playing = false;
  ready = false;
  active: number | null = null;
  reduceMotion = false;

  private viewReady = false;
  private signature = '';
  private drawTimer = 0;

  ngOnChanges(): void {
    this.reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.build();
    const next = `${this.labels.join('\u0001')}#${this.series.join(',')}`;
    if (next === this.signature) return;
    this.signature = next;
    if (this.viewReady) setTimeout(() => this.restart());
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    setTimeout(() => this.restart());
  }

  ngOnDestroy(): void {
    window.clearInterval(this.drawTimer);
  }

  average(): string {
    if (!this.points.length) return '—';
    const total = this.points.reduce((sum, point) => sum + point.value, 0);
    return `${this.figure(total / this.points.length)}${this.unit}`;
  }

  improvement(): string {
    if (this.points.length < 2) return '—';
    const first = this.points[0].value;
    const last = this.points[this.points.length - 1].value;
    if (first === 0) return this.signed(last - first);
    return `${this.signed(((last - first) / Math.abs(first)) * 100)}%`;
  }

  highest(): string {
    if (!this.points.length) return '—';
    return `${this.figure(Math.max(...this.points.map((point) => point.value)))}${this.unit}`;
  }

  // Vẽ lại từ đầu khi dữ liệu đổi hoặc người dùng bấm phát lại.
  restart(): void {
    window.clearInterval(this.drawTimer);
    const path = this.trend?.nativeElement;
    const length = path && this.points.length > 1 ? path.getTotalLength() : 0;
    this.length = length;
    this.ready = true;
    // Giấu nét và tắt class để chấm chạy lại từ đầu.
    this.playing = false;
    this.offset = this.reduceMotion || length === 0 ? 0 : length;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.playing = true;
      this.cdr.detectChanges();
    }, 30);
    if (this.reduceMotion || length === 0) return;
    const duration = 2800;
    const start = performance.now();
    // Kéo nét từ trái sang phải theo thời gian, cùng nhịp với từng chấm.
    this.drawTimer = window.setInterval(() => {
      const progress = Math.min(1, (performance.now() - start) / duration);
      this.offset = length * (1 - progress);
      this.cdr.detectChanges();
      if (progress >= 1) window.clearInterval(this.drawTimer);
    }, 16);
  }

  show(index: number): void {
    this.active = index;
  }

  hide(): void {
    this.active = null;
  }

  tipStyle(point: PlotPoint): Record<string, string> {
    const left = (point.x / this.width) * 100;
    const top = (point.y / this.height) * 100;
    const align = left > 72 ? 'translate(-100%, calc(-100% - 12px))' : 'translate(12px, calc(-100% - 12px))';
    return { left: `${left}%`, top: `${top}%`, transform: align };
  }

  short(label: string): string {
    const text = label.trim();
    return text.length > 14 ? `${text.slice(0, 13)}…` : text;
  }

  showLabel(index: number): boolean {
    const count = this.points.length;
    if (count <= 6) return true;
    return index === 0 || index === count - 1 || index % 2 === 0;
  }

  private build(): void {
    const count = Math.min(this.labels?.length ?? 0, this.series?.length ?? 0);
    if (count === 0) {
      this.points = [];
      this.ticks = [];
      this.line = '';
      this.area = '';
      return;
    }

    const values = this.series.slice(0, count);
    const min = this.yAxisMin ?? Math.min(...values);
    const max = this.yAxisMax ?? Math.max(...values);
    const span = max - min || 1;
    const innerW = this.width - this.pad.l - this.pad.r;
    const innerH = this.height - this.pad.t - this.pad.b;
    this.points = values.map((value, index) => ({
      x: count === 1 ? this.pad.l + innerW / 2 : this.pad.l + (innerW * index) / (count - 1),
      y: this.pad.t + innerH * (1 - (value - min) / span),
      label: this.labels[index],
      value,
    }));
    this.line = curve(this.points);
    const bottom = this.pad.t + innerH;
    const last = this.points[count - 1];
    const first = this.points[0];
    this.area = `${this.line} L ${last.x} ${bottom} L ${first.x} ${bottom} Z`;
    this.ticks = [0, 0.25, 0.5, 0.75, 1].map((step) => ({
      y: this.pad.t + innerH * (1 - step),
      label: this.figure(min + span * step),
    }));
  }

  private figure(value: number): string {
    const rounded = Math.round(value * 10) / 10;
    return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(1);
  }

  private signed(value: number): string {
    const text = this.figure(value);
    return value > 0 ? `+${text}` : text;
  }
}

// Nối các điểm bằng đoạn cong, không gãy tại từng mốc.
function curve(points: PlotPoint[]): string {
  if (points.length === 0) return '';
  if (points.length === 1) return `M ${points[0].x} ${points[0].y}`;
  let path = `M ${points[0].x} ${points[0].y}`;
  for (let index = 0; index < points.length - 1; index++) {
    const previous = points[index - 1] ?? points[index];
    const current = points[index];
    const next = points[index + 1];
    const after = points[index + 2] ?? next;
    const control1x = current.x + (next.x - previous.x) / 6;
    const control1y = current.y + (next.y - previous.y) / 6;
    const control2x = next.x - (after.x - current.x) / 6;
    const control2y = next.y - (after.y - current.y) / 6;
    path += ` C ${control1x} ${control1y}, ${control2x} ${control2y}, ${next.x} ${next.y}`;
  }
  return path;
}

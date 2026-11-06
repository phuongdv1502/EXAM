import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  QueryList,
  ViewChild,
  ViewChildren,
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
  private readonly zone = inject(NgZone);
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
  @ViewChild('stroke') stroke?: ElementRef<SVGPathElement>;
  @ViewChild('fill') fill?: ElementRef<SVGPathElement>;
  @ViewChildren('dot') dots?: QueryList<ElementRef<SVGGElement>>;

  readonly fillId = `perf-fill-${AnimatedLineChart.nextId}`;
  readonly glowId = `perf-glow-${AnimatedLineChart.nextId++}`;
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
  private frame = 0;

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
    cancelAnimationFrame(this.frame);
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
    cancelAnimationFrame(this.frame);
    const path = this.trend?.nativeElement;
    const length = path && this.points.length > 1 ? path.getTotalLength() : 0;
    this.length = length;
    this.ready = true;
    this.playing = false;
    this.dots?.forEach((dot) => dot.nativeElement.classList.remove('is-on'));
    // Giấu nét trước, rồi vẽ theo từng khung hình để không giật.
    this.paint(this.reduceMotion || length === 0 ? 0 : length, this.reduceMotion || length === 0 ? 1 : 0);
    this.cdr.detectChanges();
    if (this.reduceMotion || length === 0) {
      this.playing = true;
      this.markDots(1);
      this.cdr.detectChanges();
      return;
    }
    this.zone.runOutsideAngular(() => {
      const duration = 3200;
      const start = performance.now();
      const step = (now: number) => {
        const t = Math.min(1, (now - start) / duration);
        const eased = easeInOut(t);
        this.paint(length * (1 - eased), eased);
        if (t < 1) this.frame = requestAnimationFrame(step);
      };
      this.frame = requestAnimationFrame(() => {
        this.playing = true;
        this.cdr.detectChanges();
        this.frame = requestAnimationFrame(step);
      });
    });
  }

  // Ghi nét, vùng tô và chấm thẳng lên SVG, không chạy lại cả trang mỗi khung hình.
  private paint(offset: number, progress: number): void {
    this.offset = offset;
    const dash = `${this.length || 1}px`;
    const hidden = `${offset}px`;
    for (const path of [this.trend?.nativeElement, this.stroke?.nativeElement]) {
      if (!path) continue;
      path.style.strokeDasharray = dash;
      path.style.strokeDashoffset = hidden;
    }
    const fill = this.fill?.nativeElement;
    if (fill) fill.style.clipPath = `inset(0 ${(1 - progress) * 100}% 0 0)`;
    this.markDots(progress);
  }

  private markDots(progress: number): void {
    const last = this.points.length - 1;
    this.dots?.forEach((dot, index) => {
      const reached = last <= 0 || progress >= index / last;
      dot.nativeElement.classList.toggle('is-on', reached);
    });
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

// Nối các điểm bằng đường cong đơn điệu: đi qua đúng từng mốc, không gãy khúc và không móc ngược.
function curve(points: PlotPoint[]): string {
  if (points.length === 0) return '';
  if (points.length === 1) return `M ${points[0].x} ${points[0].y}`;
  const slopes = points.slice(0, -1).map((point, index) => {
    const next = points[index + 1];
    return (next.y - point.y) / (next.x - point.x || 1);
  });
  const tangents = points.map((_, index) => {
    if (index === 0) return slopes[0];
    if (index === points.length - 1) return slopes[slopes.length - 1];
    if (slopes[index - 1] * slopes[index] <= 0) return 0;
    const average = (slopes[index - 1] + slopes[index]) / 2;
    const limit = 3 * Math.min(Math.abs(slopes[index - 1]), Math.abs(slopes[index]));
    return Math.abs(average) > limit ? Math.sign(average) * limit : average;
  });
  let path = `M ${points[0].x} ${points[0].y}`;
  for (let index = 0; index < points.length - 1; index++) {
    const current = points[index];
    const next = points[index + 1];
    const span = next.x - current.x;
    const control1x = current.x + span / 3;
    const control1y = current.y + (tangents[index] * span) / 3;
    const control2x = next.x - span / 3;
    const control2y = next.y - (tangents[index + 1] * span) / 3;
    path += ` C ${round(control1x)} ${round(control1y)}, ${round(control2x)} ${round(control2y)}, ${round(next.x)} ${round(next.y)}`;
  }
  return path;
}

function round(value: number): number {
  return Math.round(value * 10) / 10;
}

// Chậm ở hai đầu, đều ở giữa, để nét chạy nhìn liền chứ không giật.
function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - ((-2 * t + 2) ** 3) / 2;
}

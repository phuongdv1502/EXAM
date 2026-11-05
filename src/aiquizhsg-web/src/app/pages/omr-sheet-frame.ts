export interface Point {
  x: number;
  y: number;
}

export type SheetMiss = 'missing' | 'far' | 'clipped' | 'skew';

export interface SheetLook {
  corners: Point[] | null;
  problem: SheetMiss | null;
  brightness: number;
  sharpness: number;
}

export const sheetPageWidth = 1200;
export const sheetPageHeight = 1697;

export function findSheet(pixels: Uint8ClampedArray, width: number, height: number): SheetLook {
  const gray = luma(pixels);
  const soft = boxBlur(gray, width, height);
  const paper = paperMask(soft, width, height);
  const blob = largestBlob(paper, width, height);
  if (!blob) return { corners: null, problem: 'missing', brightness: 0, sharpness: 0 };
  const corners = [blob.topLeft, blob.topRight, blob.bottomRight, blob.bottomLeft];
  if (blob.area / (width * height) < 0.22) return { corners, problem: 'far', brightness: 0, sharpness: 0 };
  if (clipped(corners, width, height)) return { corners, problem: 'clipped', brightness: 0, sharpness: 0 };
  if (!quadFits(corners)) return { corners, problem: 'skew', brightness: 0, sharpness: 0 };
  const stats = measure(gray, width, height, corners);
  return { corners, problem: null, brightness: stats.brightness, sharpness: stats.sharpness };
}

export function orderCorners(corners: Point[], anchor: Point | null): Point[] {
  const center = {
    x: corners.reduce((sum, point) => sum + point.x, 0) / corners.length,
    y: corners.reduce((sum, point) => sum + point.y, 0) / corners.length,
  };
  const sorted = [...corners].sort(
    (left, right) => Math.atan2(left.y - center.y, left.x - center.x) - Math.atan2(right.y - center.y, right.x - center.x),
  );
  let start = 0;
  let best = Infinity;
  sorted.forEach((point, index) => {
    const score = anchor ? Math.hypot(point.x - anchor.x, point.y - anchor.y) : point.x + point.y;
    if (score < best) {
      best = score;
      start = index;
    }
  });
  return [0, 1, 2, 3].map((step) => sorted[(start + step) % 4]);
}

export function pointInQuad(point: Point, corners: Point[]): boolean {
  let inside = false;
  for (let current = 0, previous = corners.length - 1; current < corners.length; previous = current++) {
    const a = corners[current];
    const b = corners[previous];
    const crosses = a.y > point.y !== b.y > point.y;
    const x = ((b.x - a.x) * (point.y - a.y)) / (b.y - a.y + 0.00001) + a.x;
    if (crosses && point.x < x) inside = !inside;
  }
  return inside;
}

export function homography(from: Point[], to: Point[]): number[] | null {
  const rows = Array.from({ length: 8 }, () => new Float64Array(9));
  for (let index = 0; index < 4; index++) {
    const x = from[index].x;
    const y = from[index].y;
    const u = to[index].x;
    const v = to[index].y;
    rows[index * 2].set([x, y, 1, 0, 0, 0, -u * x, -u * y, u]);
    rows[index * 2 + 1].set([0, 0, 0, x, y, 1, -v * x, -v * y, v]);
  }
  for (let column = 0; column < 8; column++) {
    let pivot = column;
    for (let row = column + 1; row < 8; row++) {
      if (Math.abs(rows[row][column]) > Math.abs(rows[pivot][column])) pivot = row;
    }
    if (Math.abs(rows[pivot][column]) < 1e-8) return null;
    if (pivot !== column) {
      const swap = rows[column];
      rows[column] = rows[pivot];
      rows[pivot] = swap;
    }
    const divisor = rows[column][column];
    for (let columnIndex = column; columnIndex < 9; columnIndex++) rows[column][columnIndex] /= divisor;
    for (let row = 0; row < 8; row++) {
      if (row === column) continue;
      const factor = rows[row][column];
      for (let columnIndex = column; columnIndex < 9; columnIndex++) rows[row][columnIndex] -= factor * rows[column][columnIndex];
    }
  }
  return rows.map((row) => row[8]);
}

export function mapPoint(matrix: number[], point: Point): Point {
  const denominator = matrix[6] * point.x + matrix[7] * point.y + 1;
  return {
    x: (matrix[0] * point.x + matrix[1] * point.y + matrix[2]) / denominator,
    y: (matrix[3] * point.x + matrix[4] * point.y + matrix[5]) / denominator,
  };
}

export function warpSheet(source: ImageData, corners: Point[], destWidth: number, destHeight: number): ImageData | null {
  const destination = [
    { x: 0, y: 0 },
    { x: destWidth - 1, y: 0 },
    { x: destWidth - 1, y: destHeight - 1 },
    { x: 0, y: destHeight - 1 },
  ];
  const matrix = homography(destination, corners);
  if (!matrix) return null;
  const output = new ImageData(destWidth, destHeight);
  const from = source.data;
  const to = output.data;
  const width = source.width;
  const height = source.height;
  for (let y = 0; y < destHeight; y++) {
    for (let x = 0; x < destWidth; x++) {
      const denominator = matrix[6] * x + matrix[7] * y + 1;
      const sourceX = (matrix[0] * x + matrix[1] * y + matrix[2]) / denominator;
      const sourceY = (matrix[3] * x + matrix[4] * y + matrix[5]) / denominator;
      const offset = (y * destWidth + x) * 4;
      if (sourceX < 0 || sourceY < 0 || sourceX >= width - 1 || sourceY >= height - 1) {
        to[offset] = 255;
        to[offset + 1] = 255;
        to[offset + 2] = 255;
        to[offset + 3] = 255;
        continue;
      }
      const x0 = Math.floor(sourceX);
      const y0 = Math.floor(sourceY);
      const dx = sourceX - x0;
      const dy = sourceY - y0;
      const index00 = (y0 * width + x0) * 4;
      const index10 = index00 + 4;
      const index01 = index00 + width * 4;
      const index11 = index01 + 4;
      for (let channel = 0; channel < 4; channel++) {
        to[offset + channel] =
          from[index00 + channel] * (1 - dx) * (1 - dy) +
          from[index10 + channel] * dx * (1 - dy) +
          from[index01 + channel] * (1 - dx) * dy +
          from[index11 + channel] * dx * dy;
      }
    }
  }
  return output;
}

function luma(pixels: Uint8ClampedArray): Uint8Array {
  const gray = new Uint8Array(pixels.length / 4);
  for (let index = 0; index < gray.length; index++) {
    const offset = index * 4;
    gray[index] = (pixels[offset] * 0.299 + pixels[offset + 1] * 0.587 + pixels[offset + 2] * 0.114) | 0;
  }
  return gray;
}

function boxBlur(source: Uint8Array, width: number, height: number): Uint8Array {
  const blurred = new Uint8Array(source.length);
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      let sum = 0;
      let count = 0;
      for (let offsetY = -1; offsetY <= 1; offsetY++) {
        const nextY = y + offsetY;
        if (nextY < 0 || nextY >= height) continue;
        for (let offsetX = -1; offsetX <= 1; offsetX++) {
          const nextX = x + offsetX;
          if (nextX < 0 || nextX >= width) continue;
          sum += source[nextY * width + nextX];
          count++;
        }
      }
      blurred[y * width + x] = sum / count;
    }
  }
  return blurred;
}

function paperMask(gray: Uint8Array, width: number, height: number): Uint8Array {
  const histogram = new Uint32Array(256);
  for (const value of gray) histogram[value]++;
  let brighter = 0;
  let strong = 180;
  for (let tone = 255; tone >= 0; tone--) {
    brighter += histogram[tone];
    if (brighter < gray.length * 0.38) continue;
    strong = Math.max(140, tone);
    break;
  }
  const weak = Math.max(1, strong - 36);
  const paper = new Uint8Array(gray.length);
  const stack: number[] = [];
  const push = (x: number, y: number) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const index = y * width + x;
    if (paper[index] || gray[index] < weak) return;
    paper[index] = 1;
    stack.push(index);
  };
  for (let index = 0; index < gray.length; index++) {
    if (gray[index] < strong) continue;
    push(index % width, (index - (index % width)) / width);
  }
  while (stack.length) {
    const index = stack.pop() as number;
    const x = index % width;
    const y = (index - x) / width;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }
  return paper;
}

interface BlobCorners {
  area: number;
  topLeft: Point;
  topRight: Point;
  bottomRight: Point;
  bottomLeft: Point;
}

function largestBlob(paper: Uint8Array, width: number, height: number): BlobCorners | null {
  const seen = new Uint8Array(paper.length);
  const stack: number[] = [];
  let best: BlobCorners | null = null;
  for (let start = 0; start < paper.length; start++) {
    if (!paper[start] || seen[start]) continue;
    seen[start] = 1;
    stack.push(start);
    let area = 0;
    let minSum = Infinity;
    let maxSum = -Infinity;
    let minDiff = Infinity;
    let maxDiff = -Infinity;
    let topLeft: Point = { x: 0, y: 0 };
    let topRight: Point = { x: 0, y: 0 };
    let bottomRight: Point = { x: 0, y: 0 };
    let bottomLeft: Point = { x: 0, y: 0 };
    while (stack.length) {
      const index = stack.pop() as number;
      const x = index % width;
      const y = (index - x) / width;
      area++;
      const sum = x + y;
      const diff = x - y;
      if (sum < minSum) {
        minSum = sum;
        topLeft = { x, y };
      }
      if (sum > maxSum) {
        maxSum = sum;
        bottomRight = { x, y };
      }
      if (diff > maxDiff) {
        maxDiff = diff;
        topRight = { x, y };
      }
      if (diff < minDiff) {
        minDiff = diff;
        bottomLeft = { x, y };
      }
      const consider = (nextX: number, nextY: number) => {
        if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height) return;
        const next = nextY * width + nextX;
        if (seen[next] || !paper[next]) return;
        seen[next] = 1;
        stack.push(next);
      };
      consider(x + 1, y);
      consider(x - 1, y);
      consider(x, y + 1);
      consider(x, y - 1);
    }
    if (!best || area > best.area) best = { area, topLeft, topRight, bottomRight, bottomLeft };
  }
  return best;
}

function clipped(corners: Point[], width: number, height: number): boolean {
  const left = corners.filter((point) => point.x <= 0).length;
  const right = corners.filter((point) => point.x >= width - 1).length;
  const top = corners.filter((point) => point.y <= 0).length;
  const bottom = corners.filter((point) => point.y >= height - 1).length;
  return left >= 2 || right >= 2 || top >= 2 || bottom >= 2;
}

function quadFits(corners: Point[]): boolean {
  for (let left = 0; left < 4; left++) {
    for (let right = left + 1; right < 4; right++) {
      if (Math.hypot(corners[left].x - corners[right].x, corners[left].y - corners[right].y) < 6) return false;
    }
  }
  let sign = 0;
  for (let index = 0; index < 4; index++) {
    const cross = crossAt(corners[index], corners[(index + 1) % 4], corners[(index + 2) % 4]);
    if (Math.abs(cross) < 1) return false;
    const next = Math.sign(cross);
    if (sign && next !== sign) return false;
    sign = next;
  }
  const sides = [0, 1, 2, 3].map((index) =>
    Math.hypot(corners[(index + 1) % 4].x - corners[index].x, corners[(index + 1) % 4].y - corners[index].y),
  );
  const longest = Math.max(...sides);
  const shortest = Math.min(...sides);
  if (shortest < 1 || longest / shortest > 3.4) return false;
  for (let index = 0; index < 4; index++) {
    const degrees = angleAt(corners[(index + 3) % 4], corners[index], corners[(index + 1) % 4]);
    if (degrees < 40 || degrees > 145) return false;
  }
  return true;
}

function crossAt(a: Point, b: Point, c: Point): number {
  return (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
}

function angleAt(a: Point, b: Point, c: Point): number {
  const leftX = a.x - b.x;
  const leftY = a.y - b.y;
  const rightX = c.x - b.x;
  const rightY = c.y - b.y;
  const length = Math.hypot(leftX, leftY) * Math.hypot(rightX, rightY);
  if (length < 1) return 0;
  const cosine = Math.min(1, Math.max(-1, (leftX * rightX + leftY * rightY) / length));
  return (Math.acos(cosine) * 180) / Math.PI;
}

function measure(
  gray: Uint8Array,
  width: number,
  height: number,
  corners: Point[],
): { brightness: number; sharpness: number } {
  const minX = Math.max(1, Math.floor(Math.min(...corners.map((point) => point.x))));
  const maxX = Math.min(width - 2, Math.ceil(Math.max(...corners.map((point) => point.x))));
  const minY = Math.max(1, Math.floor(Math.min(...corners.map((point) => point.y))));
  const maxY = Math.min(height - 2, Math.ceil(Math.max(...corners.map((point) => point.y))));
  let sum = 0;
  let count = 0;
  let lapSum = 0;
  let lapSquare = 0;
  for (let y = minY; y <= maxY; y++) {
    for (let x = minX; x <= maxX; x++) {
      if (!pointInQuad({ x, y }, corners)) continue;
      const value = gray[y * width + x];
      sum += value;
      count++;
      const lap =
        gray[(y - 1) * width + x] + gray[(y + 1) * width + x] + gray[y * width + (x - 1)] + gray[y * width + (x + 1)] - 4 * value;
      lapSum += lap;
      lapSquare += lap * lap;
    }
  }
  if (!count) return { brightness: 0, sharpness: 0 };
  const mean = lapSum / count;
  return { brightness: sum / count, sharpness: lapSquare / count - mean * mean };
}

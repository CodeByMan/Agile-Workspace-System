import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export interface WorkspaceChartSeries {
  name?: string;
  data: Array<number | null | undefined>;
}

export interface ApexChartOptions {
  chart?: {
    type?: 'bar' | 'radialBar' | 'area' | 'line';
    height?: number;
    [key: string]: unknown;
  };
  colors?: string[];
  dataLabels?: Record<string, unknown>;
  fill?: Record<string, unknown>;
  legend?: Record<string, unknown>;
  plotOptions?: Record<string, unknown>;
  stroke?: Record<string, unknown>;
  xaxis?: {
    categories?: Array<string | number>;
    [key: string]: unknown;
  };
  yaxis?: Record<string, unknown> | Array<Record<string, unknown>>;
  labels?: string[];
  series?: WorkspaceChartSeries[] | number[];
}

interface BarDatum {
  label: string;
  value: number;
  height: number;
}

interface LineMarker {
  x: number;
  y: number;
}

interface LinePath {
  name: string;
  color: string;
  points: string;
  areaPoints: string;
  markers: LineMarker[];
}

interface AxisLabel {
  text: string;
  position: number;
}

@Component({
  selector: 'app-apex-chart',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './apex-chart.component.html',
  styleUrl: './apex-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApexChartComponent {
  @Input({ required: true }) options: ApexChartOptions = {};

  readonly palette = ['#1A73E8', '#34A853', '#FBBC04', '#EA4335'];

  chartType(): 'bar' | 'radialBar' | 'area' | 'line' {
    return this.options.chart?.type ?? 'line';
  }

  chartHeight(): number {
    const configured = Number(this.options.chart?.height ?? 300);
    return Number.isFinite(configured) ? Math.max(220, configured) : 300;
  }

  primaryColor(): string {
    return this.options.colors?.[0] ?? this.palette[0];
  }

  radialValue(): number {
    const series = this.options.series;
    if (!Array.isArray(series) || typeof series[0] !== 'number') {
      return 0;
    }

    const value = Number(series[0]);
    return Number.isFinite(value) ? Math.min(100, Math.max(0, value)) : 0;
  }

  radialLabel(): string {
    return this.options.labels?.[0] ?? 'Completion';
  }

  barData(): BarDatum[] {
    const series = this.axisSeries()[0];
    if (!series) {
      return [];
    }

    const values = series.data.map((value) => this.safeNumber(value));
    const maximum = Math.max(1, ...values);
    const categories = this.categories();

    return values.map((value, index) => ({
      label: categories[index] ?? `Item ${index + 1}`,
      value,
      height: Math.max(value > 0 ? 5 : 0, (value / maximum) * 100),
    }));
  }

  linePaths(): LinePath[] {
    const series = this.axisSeries();
    if (!series.length) {
      return [];
    }

    const values = series.flatMap((item) => item.data.map((value) => this.safeNumber(value)));
    const maximum = Math.max(1, ...values);
    const width = 1000;
    const top = 24;
    const bottom = 248;
    const left = 26;
    const right = width - 26;

    return series.map((item, seriesIndex) => {
      const data = item.data.map((value) => this.safeNumber(value));
      const denominator = Math.max(1, data.length - 1);
      const markers = data.map((value, index) => ({
        x: data.length === 1 ? width / 2 : left + ((right - left) * index) / denominator,
        y: bottom - ((bottom - top) * value) / maximum,
      }));
      const points = markers.map((point) => `${point.x},${point.y}`).join(' ');
      const firstX = markers[0]?.x ?? left;
      const lastX = markers.at(-1)?.x ?? right;

      return {
        name: item.name ?? `Series ${seriesIndex + 1}`,
        color:
          this.options.colors?.[seriesIndex] ?? this.palette[seriesIndex % this.palette.length],
        points,
        areaPoints: `${firstX},${bottom} ${points} ${lastX},${bottom}`,
        markers,
      };
    });
  }

  axisLabels(): AxisLabel[] {
    const categories = this.categories();
    if (!categories.length) {
      return [];
    }

    const maximumLabels = 6;
    if (categories.length <= maximumLabels) {
      return categories.map((text, index) => ({
        text,
        position: categories.length === 1 ? 50 : (index / (categories.length - 1)) * 100,
      }));
    }

    const indexes = new Set<number>();
    for (let index = 0; index < maximumLabels; index += 1) {
      indexes.add(Math.round((index * (categories.length - 1)) / (maximumLabels - 1)));
    }

    return [...indexes].map((index) => ({
      text: categories[index],
      position: (index / (categories.length - 1)) * 100,
    }));
  }

  hasAxisData(): boolean {
    return this.axisSeries().some((series) => series.data.length > 0);
  }

  legendVisible(): boolean {
    return this.axisSeries().length > 1;
  }

  ariaLabel(): string {
    if (this.chartType() === 'radialBar') {
      return `${this.radialLabel()}: ${Math.round(this.radialValue())} percent`;
    }

    const names = this.axisSeries()
      .map((series) => series.name)
      .filter(Boolean)
      .join(' and ');
    return names ? `${names} chart` : 'Workspace data chart';
  }

  private axisSeries(): WorkspaceChartSeries[] {
    const series = this.options.series;
    if (!Array.isArray(series) || !series.length || typeof series[0] === 'number') {
      return [];
    }

    return (series as WorkspaceChartSeries[]).map((item) => ({
      name: item?.name,
      data: Array.isArray(item?.data) ? item.data : [],
    }));
  }

  private categories(): string[] {
    return (this.options.xaxis?.categories ?? []).map((item) => String(item));
  }

  private safeNumber(value: number | null | undefined): number {
    const numeric = Number(value ?? 0);
    return Number.isFinite(numeric) ? Math.max(0, numeric) : 0;
  }
}

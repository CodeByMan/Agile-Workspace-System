import { TestBed } from '@angular/core/testing';
import { ApexChartComponent } from './apex-chart.component';

describe('ApexChartComponent Angular-native renderer', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ApexChartComponent] }).compileComponents();
  });

  it('renders a radial progress chart without a third-party chart runtime', () => {
    const fixture = TestBed.createComponent(ApexChartComponent);
    fixture.componentRef.setInput('options', {
      chart: { type: 'radialBar', height: 260 },
      series: [72],
      labels: ['Completion'],
      colors: ['#1A73E8'],
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.radial-chart__ring')).not.toBeNull();
    expect(element.textContent).toContain('72%');
    expect(element.textContent).toContain('Completion');
  });

  it('renders safe empty state when an axis chart has no data', () => {
    const fixture = TestBed.createComponent(ApexChartComponent);
    fixture.componentRef.setInput('options', {
      chart: { type: 'area', height: 300 },
      series: [{ name: 'Work', data: [] }],
    });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.chart-empty')).not.toBeNull();
  });

  it('normalizes invalid numeric values instead of throwing', () => {
    const fixture = TestBed.createComponent(ApexChartComponent);
    fixture.componentRef.setInput('options', {
      chart: { type: 'bar', height: 280 },
      series: [{ name: 'Tasks', data: [4, Number.NaN, -3, 8] }],
      xaxis: { categories: ['Ready', 'Unknown', 'Blocked', 'Done'] },
      colors: ['#1A73E8'],
    });

    expect(() => fixture.detectChanges()).not.toThrow();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('.bar-chart__column')).toHaveLength(4);
    expect(element.textContent).toContain('Done');
  });
});

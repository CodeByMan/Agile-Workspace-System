import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { NotificationSummary } from '../../core/models/notification.models';
import {
  DashboardSummary,
  ProjectProgress,
  RecentActivity,
  UserProductivity,
} from '../../core/models/dashboard.models';
import { ApexChartComponent, ApexChartOptions } from '../../shared/apex-chart.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, ApexChartComponent],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly notificationsService = inject(NotificationsService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly projects = signal<ProjectProgress[]>([]);
  readonly activity = signal<RecentActivity[]>([]);
  readonly productivity = signal<UserProductivity[]>([]);
  readonly notifications = signal<NotificationSummary | null>(null);
  readonly statusChart = signal<ApexChartOptions>({
    series: [{ name: 'Tasks', data: [] }],
    chart: { type: 'bar', height: 280 },
  });
  readonly storyChart = signal<ApexChartOptions>({
    series: [0],
    chart: { type: 'radialBar', height: 280 },
  });
  readonly throughputChart = signal<ApexChartOptions>({
    series: [{ name: 'Completed Story Points', data: [] }],
    chart: { type: 'area', height: 320 },
  });

  ngOnInit(): void {
    forkJoin({
      summary: this.dashboardService.getSummary(),
      projects: this.dashboardService.getProjectProgress(),
      activity: this.dashboardService.getRecentActivity(10),
      productivity: this.dashboardService.getUserProductivity().pipe(catchError(() => of([]))),
      notifications: this.notificationsService
        .get(5)
        .pipe(catchError(() => of({ Total: 0, Unread: 0, Items: [] }))),
    }).subscribe({
      next: (result) => {
        this.summary.set(result.summary);
        this.projects.set(result.projects);
        this.activity.set(result.activity);
        this.productivity.set(result.productivity);
        this.notifications.set(result.notifications);
        this.buildCharts(result.summary, result.projects, result.productivity);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load dashboard data right now.');
        this.loading.set(false);
      },
    });
  }

  private buildCharts(
    summary: DashboardSummary,
    projects: ProjectProgress[],
    productivity: UserProductivity[],
  ): void {
    this.statusChart.set({
      series: [{ name: 'Tasks', data: summary.TasksByStatus.map((item) => item.Count) }],
      chart: { type: 'bar', height: 280 },
      plotOptions: { bar: { borderRadius: 3, columnWidth: '52%' } },
      dataLabels: { enabled: false },
      xaxis: { categories: summary.TasksByStatus.map((item) => item.Status) },
      colors: ['#1A73E8'],
    });

    this.storyChart.set({
      series: [Number(summary.CompletionRate ?? 0)],
      chart: { type: 'radialBar', height: 280 },
      plotOptions: {
        radialBar: {
          hollow: { size: '64%' },
          dataLabels: {
            name: { show: false },
            value: { fontSize: '28px', fontWeight: '700', offsetY: 8 },
          },
        },
      },
      colors: ['#34A853'],
      labels: ['Completion'],
    });

    const topRows = (
      productivity.length
        ? productivity
        : projects.map(
            (project) =>
              ({
                FullName: project.ProjectName,
                CompletedStoryPoints: project.CompletedStoryPoints,
              }) as UserProductivity,
          )
    ).slice(0, 6);

    this.throughputChart.set({
      series: [
        { name: 'Completed Story Points', data: topRows.map((item) => item.CompletedStoryPoints) },
      ],
      chart: { type: 'area', height: 320 },
      stroke: { curve: 'smooth', width: 3 },
      dataLabels: { enabled: false },
      xaxis: { categories: topRows.map((item) => item.FullName) },
      fill: { opacity: 0.16 },
      legend: { position: 'top', horizontalAlign: 'left' },
      colors: ['#1A73E8'],
    });
  }

  completionRate(project: ProjectProgress): number {
    return Number(project.CompletionRate ?? 0);
  }
}

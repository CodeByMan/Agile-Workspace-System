import { TaskPriority, WorkStatus } from './task.models';

describe('work-item API enum contract', () => {
  it('uses the status values accepted by the ASP.NET API', () => {
    expect(WorkStatus.ToDo).toBe(1);
    expect(WorkStatus.InProgress).toBe(2);
    expect(WorkStatus.Done).toBe(3);
    expect(WorkStatus.Blocked).toBe(4);
  });

  it('uses the priority values accepted by the ASP.NET API', () => {
    expect(TaskPriority.Low).toBe(1);
    expect(TaskPriority.Medium).toBe(2);
    expect(TaskPriority.High).toBe(3);
    expect(TaskPriority.Critical).toBe(4);
  });
});

export type WorkspaceRole = 'Admin' | 'ScrumMaster' | 'Manager' | 'TeamLead' | 'Developer';

export const ROLES: readonly WorkspaceRole[] = [
  'Admin',
  'ScrumMaster',
  'Manager',
  'TeamLead',
  'Developer',
];

const rank: Record<WorkspaceRole, number> = {
  Admin: 500,
  ScrumMaster: 400,
  Manager: 300,
  TeamLead: 200,
  Developer: 100,
};

export function isWorkspaceRole(value: string): value is WorkspaceRole {
  return (ROLES as readonly string[]).includes(value);
}

export function canManageRole(actor: WorkspaceRole, target: WorkspaceRole): boolean {
  return actor === 'Admin' || rank[actor] > rank[target];
}

export function canAssignRole(actor: WorkspaceRole, requested: WorkspaceRole): boolean {
  if (requested === 'Admin') {
    return actor === 'Admin';
  }

  return actor === 'Admin' || rank[actor] > rank[requested];
}

export function assignableRoles(actor: WorkspaceRole): WorkspaceRole[] {
  return ROLES.filter((role) => canAssignRole(actor, role));
}

export function canManageWorkItemPlanning(role: WorkspaceRole): boolean {
  return role !== 'Developer';
}

export function backlogCapabilities(role: WorkspaceRole) {
  return {
    create: canManageWorkItemPlanning(role),
    update: true,
    delete: ['Admin', 'ScrumMaster', 'Manager'].includes(role),
  };
}

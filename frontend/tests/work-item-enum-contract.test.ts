import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const currentDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(currentDirectory, '..', '..');

function parseEnum(source: string, enumName: string): Record<string, number> {
  const enumPattern = new RegExp(`(?:export\\s+)?enum\\s+${enumName}\\s*\\{([\\s\\S]*?)\\}`);
  const enumMatch = source.match(enumPattern);

  assert.ok(enumMatch, `Expected enum ${enumName} to exist.`);

  const values: Record<string, number> = {};
  const memberPattern = /([A-Za-z][A-Za-z0-9_]*)\s*=\s*(-?\d+)/g;
  let memberMatch: RegExpExecArray | null;

  while ((memberMatch = memberPattern.exec(enumMatch[1])) !== null) {
    values[memberMatch[1]] = Number(memberMatch[2]);
  }

  return values;
}

test('Angular work-item status values match the ASP.NET API contract', () => {
  const frontend = readFileSync(
    resolve(repositoryRoot, 'frontend/src/app/core/models/task.models.ts'),
    'utf8',
  );
  const backend = readFileSync(
    resolve(repositoryRoot, 'backend/api/Models/Enums/TaskStatus.cs'),
    'utf8',
  );

  assert.deepEqual(parseEnum(frontend, 'WorkStatus'), parseEnum(backend, 'TaskStatus'));
});

test('Angular work-item priority values match the ASP.NET API contract', () => {
  const frontend = readFileSync(
    resolve(repositoryRoot, 'frontend/src/app/core/models/task.models.ts'),
    'utf8',
  );
  const backend = readFileSync(
    resolve(repositoryRoot, 'backend/api/Models/Enums/TaskPriority.cs'),
    'utf8',
  );

  assert.deepEqual(parseEnum(frontend, 'TaskPriority'), parseEnum(backend, 'TaskPriority'));
});

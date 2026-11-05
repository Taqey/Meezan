import fs from 'node:fs';
import path from 'node:path';

const SRC = 'frontend/src/app/shared/models';
const raw = fs.readFileSync(path.join(SRC, 'fund-display.ts'), 'utf8');
const lines = raw.split(/\r?\n/);
const EOL = '\r\n';

function slice(start1, end1) {
  return lines.slice(start1 - 1, end1).join(EOL) + EOL;
}

// 1. fund-normalization.util.ts (lines 1-274)
const normCode = [
  `import { environment } from '../../../environments/environment';`,
  `import type { FundType, FundRiskLevel } from './api.models';`,
  ``,
  slice(12, 274)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'fund-normalization.util.ts'), normCode, 'utf8');

// 2. fund-manager.util.ts (lines 276-313)
const managerCode = [
  slice(276, 313)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'fund-manager.util.ts'), managerCode, 'utf8');

// 3. fund-allocation.model.ts (lines 315-345)
const allocModelCode = [
  slice(315, 345)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'fund-allocation.model.ts'), allocModelCode, 'utf8');

// 4. fund-allocation.util.ts (lines 346-750)
const allocUtilCode = [
  `import type { FundAllocationBand, FundAllocationCardItem, FundAllocationModel } from './fund-allocation.model';`,
  ``,
  slice(346, 750)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'fund-allocation.util.ts'), allocUtilCode, 'utf8');

// 5. Facade in fund-display.ts
const facade = [
  `/**`,
  ` * Display-layer normalization and parsing for investment funds.`,
  ` * Re-exports split modules:`,
  ` * - fund-normalization.util.ts`,
  ` * - fund-manager.util.ts`,
  ` * - fund-allocation.model.ts`,
  ` * - fund-allocation.util.ts`,
  ` */`,
  ``,
  `export * from './fund-normalization.util';`,
  `export * from './fund-manager.util';`,
  `export * from './fund-allocation.model';`,
  `export * from './fund-allocation.util';`,
  ``
].join(EOL);

fs.writeFileSync(path.join(SRC, 'fund-display.ts'), facade, 'utf8');
console.log('Successfully split fund-display.ts!');


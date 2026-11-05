import fs from 'node:fs';
import path from 'node:path';

const SRC = 'frontend/src/app/shared/models';
const raw = fs.readFileSync(path.join(SRC, 'shariah-standards.ts'), 'utf8');
const lines = raw.split(/\r?\n/);
const EOL = '\r\n';

function slice(start1, end1) {
  return lines.slice(start1 - 1, end1).join(EOL) + EOL;
}

// 1. shariah-standards.model.ts (lines 1-128, 138-171, 244-245, 257-275, 396-412)
const modelCode = [
  slice(1, 128),
  slice(138, 171),
  slice(244, 245),
  slice(257, 275),
  slice(396, 412)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'shariah-standards.model.ts'), modelCode, 'utf8');

// 2. shariah-evaluator.util.ts (lines 130-136, 173-242, 246-255, 277-394, 414-508)
const evalCode = [
  `import type {`,
  `  ShariahCriterionKey, ShariahCriterionLabel, ShariahStandard,`,
  `  StockRatios, CriterionEvaluation, StandardEvaluation, StandardsEvaluation,`,
  `  NormalizedStatus, SourceStatusInput, EffectiveSourceStatus, NonComplianceReasonInput`,
  `} from './shariah-standards.model';`,
  `import {`,
  `  CRITERION_ORDER, CRITERION_LABELS, SHARIAH_STANDARDS,`,
  `  scholarsDebtMax, SCHOLARS_COMPLIANT_VERDICT`,
  `} from './shariah-standards.model';`,
  ``,
  slice(130, 136),
  slice(173, 242),
  slice(246, 255),
  slice(277, 394),
  slice(414, 508)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'shariah-evaluator.util.ts'), evalCode, 'utf8');

// 3. Facade in shariah-standards.ts
const facade = [
  `/**`,
  ` * Shariah Standards and Criteria Evaluations.`,
  ` * Re-exports:`,
  ` * - shariah-standards.model.ts`,
  ` * - shariah-evaluator.util.ts`,
  ` */`,
  ``,
  `export * from './shariah-standards.model';`,
  `export * from './shariah-evaluator.util';`,
  ``
].join(EOL);

fs.writeFileSync(path.join(SRC, 'shariah-standards.ts'), facade, 'utf8');
console.log('Successfully split shariah-standards.ts!');


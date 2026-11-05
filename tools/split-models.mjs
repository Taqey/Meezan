import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

const SRC = 'frontend/src/app/shared/models';
const raw = execSync('git show HEAD:frontend/src/app/shared/models/api.models.ts').toString('utf8');
const lines = raw.split(/\r?\n/);
const EOL = '\r\n';

function slice(start1, end1) {
  return lines.slice(start1 - 1, end1).join(EOL) + EOL;
}

// 1. common.model.ts (lines 1-12, 59-65)
const commonModel = [
  slice(1, 12),
  slice(59, 65)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'common.model.ts'), commonModel, 'utf8');

// 2. shariah.model.ts (lines 13-46, 171-184, 271-314, 407-436, 575-607)
const shariahModel = [
  `import type { ShariahStatus } from './common.model';`,
  ``,
  slice(13, 46),
  slice(171, 184),
  slice(271, 314),
  slice(407, 436),
  slice(575, 607)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'shariah.model.ts'), shariahModel, 'utf8');

// 3. sectors.model.ts (lines 127-152, 629-633)
const sectorsModel = [
  slice(127, 152),
  slice(629, 633)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'sectors.model.ts'), sectorsModel, 'utf8');

// 4. indices.model.ts (lines 48-57, 106-125, 154-162, 372-405, 635-640)
const indicesModel = [
  `import type { PagedResult } from './common.model';`,
  `import type { ShariahSourceOpinionDto } from './shariah.model';`,
  ``,
  slice(48, 57),
  slice(106, 125),
  slice(154, 162),
  slice(372, 405),
  slice(635, 640)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'indices.model.ts'), indicesModel, 'utf8');

// 5. stocks.model.ts (lines 67-104, 164-169, 186-269, 316-329)
const stocksModel = [
  `import type { ShariahStatus, PriceComparison, ValuationConfidence } from './common.model';`,
  `import type { IndexInStockDto } from './indices.model';`,
  `import type { ShariahSourceOpinionDto, RatioCheckDto, CriterionResultDto, StoredVerdictDetailsDto, StockShariahMetricsDto } from './shariah.model';`,
  ``,
  slice(67, 104),
  slice(164, 169),
  slice(186, 269),
  slice(316, 329)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'stocks.model.ts'), stocksModel, 'utf8');

// 6. funds.model.ts (lines 669-1292, 1301-1327)
const fundsModel = [
  slice(669, 1292),
  slice(1301, 1327)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'funds.model.ts'), fundsModel, 'utf8');

// 7. admin.model.ts (lines 331-370, 438-573, 609-627, 642-667, 1294-1299)
const adminModel = [
  `import type { ShariahStatus } from './common.model';`,
  `import type { ShariahSourceOpinionDto, RatioCheckDto } from './shariah.model';`,
  ``,
  slice(331, 370),
  slice(438, 573),
  slice(609, 627),
  slice(642, 667),
  slice(1294, 1299)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'admin.model.ts'), adminModel, 'utf8');

// Rewrite api.models.ts as clean facade barrel re-exporting all domains
const barrel = [
  `/**`,
  ` * Domain Model Facade`,
  ` * Re-exports domain models split by responsibility:`,
  ` * - common.model.ts`,
  ` * - stocks.model.ts`,
  ` * - sectors.model.ts`,
  ` * - indices.model.ts`,
  ` * - shariah.model.ts`,
  ` * - funds.model.ts`,
  ` * - admin.model.ts`,
  ` */`,
  ``,
  `export * from './common.model';`,
  `export * from './stocks.model';`,
  `export * from './sectors.model';`,
  `export * from './indices.model';`,
  `export * from './shariah.model';`,
  `export * from './funds.model';`,
  `export * from './admin.model';`,
  ``
].join(EOL);

fs.writeFileSync(path.join(SRC, 'api.models.ts'), barrel, 'utf8');
console.log('Successfully re-split api.models.ts into 7 domain model files!');

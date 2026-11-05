import fs from 'node:fs';
import path from 'node:path';

const SRC = 'frontend/src/app/shared/models';
const raw = fs.readFileSync(path.join(SRC, 'market-icons.ts'), 'utf8');
const lines = raw.split(/\r?\n/);
const EOL = '\r\n';

function slice(start1, end1) {
  return lines.slice(start1 - 1, end1).join(EOL) + EOL;
}

// 1. index-display.util.ts (lines 1-154)
const indexCode = [
  slice(1, 154)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'index-display.util.ts'), indexCode, 'utf8');

// 2. sector-display.util.ts (imports + lines 156-271)
const sectorCode = [
  `import {`,
  `  LucideIconData,`,
  `  Landmark, Building2, ShoppingCart, Activity, HardHat, Tv, Phone, Zap,`,
  `  Coins, Fuel, Compass, Shield, HelpCircle`,
  `} from 'lucide-angular';`,
  ``,
  slice(156, 271)
].join(EOL);
fs.writeFileSync(path.join(SRC, 'sector-display.util.ts'), sectorCode, 'utf8');

// 3. Facade in market-icons.ts
const facade = [
  `/**`,
  ` * Visual Accents and Icons for Market Sectors and Indices.`,
  ` * Re-exports:`,
  ` * - index-display.util.ts`,
  ` * - sector-display.util.ts`,
  ` */`,
  ``,
  `export * from './index-display.util';`,
  `export * from './sector-display.util';`,
  ``
].join(EOL);

fs.writeFileSync(path.join(SRC, 'market-icons.ts'), facade, 'utf8');
console.log('Successfully split market-icons.ts!');


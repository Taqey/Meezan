import fs from 'node:fs';
import path from 'node:path';

function walk(dir) {
  let res = [];
  for (const f of fs.readdirSync(dir)) {
    const p = path.join(dir, f);
    if (fs.statSync(p).isDirectory()) res.push(...walk(p));
    else if (p.endsWith('.html')) {
      const cnt = (fs.readFileSync(p, 'utf8').match(/\sstyle="[^"]+"/g) || []).length;
      if (cnt > 0) res.push({ file: p.replace(/\\/g, '/'), count: cnt });
    }
  }
  return res;
}

const list = walk('frontend/src/app').sort((a,b) => b.count - a.count);
for (const item of list) {
  console.log(`${item.count.toString().padStart(3, ' ')}  ${item.file}`);
}


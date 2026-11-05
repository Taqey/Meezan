import fs from 'node:fs';
import path from 'node:path';

function walk(dir) {
  let res = [];
  for (const f of fs.readdirSync(dir)) {
    const p = path.join(dir, f);
    if (fs.statSync(p).isDirectory()) res.push(...walk(p));
    else if (p.endsWith('.html')) res.push(p);
  }
  return res;
}

const htmlFiles = walk('frontend/src/app');
let fixedCount = 0;

for (const file of htmlFiles) {
  let content = fs.readFileSync(file, 'utf8');
  if (!/__INL(?:INE)?_CLS__/.test(content)) continue;

  // Replace tags that have class and __INL_CLS__
  content = content.replace(/<([a-zA-Z0-9-]+)([^>]*?)\bclass="([^"]*)"([^>]*?)\b__INL(?:INE)?_CLS__="([^"]+)"([^>]*?)>/g, (m, tag, b1, cls, b2, inlCls, a) => {
    return `<${tag}${b1}class="${cls} ${inlCls}"${b2}${a}>`;
  });

  // Replace tags with __INL_CLS__ before class
  content = content.replace(/<([a-zA-Z0-9-]+)([^>]*?)\b__INL(?:INE)?_CLS__="([^"]+)"([^>]*?)\bclass="([^"]*)"([^>]*?)>/g, (m, tag, b1, inlCls, b2, cls, a) => {
    return `<${tag}${b1}class="${cls} ${inlCls}"${b2}${a}>`;
  });

  // Replace tags without class
  content = content.replace(/\b__INL(?:INE)?_CLS__="([^"]+)"/g, 'class="$1"');

  fs.writeFileSync(file, content, 'utf8');
  fixedCount++;
  console.log('Cleaned placeholder from:', file);
}

console.log(`Total cleaned: ${fixedCount}`);


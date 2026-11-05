const fs = require('fs');

const html = fs.readFileSync('tools/starta_page.html', 'utf8');

// Check if there is a table
console.log('Includes <table:', html.includes('<table'));
console.log('Includes <tbody:', html.includes('<tbody'));
console.log('Includes COMI:', html.includes('COMI'));

// Find where COMI appears
let idx = 0;
while ((idx = html.indexOf('COMI', idx)) !== -1) {
  console.log('COMI occurrence at:', idx);
  console.log(html.substring(Math.max(0, idx - 150), Math.min(html.length, idx + 350)));
  console.log('---');
  idx += 4;
  if (idx > 100000) break; // only first few
}


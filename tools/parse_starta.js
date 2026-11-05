const fs = require('fs');

const html = fs.readFileSync('tools/starta_page.html', 'utf8');

// Find table tbody
const tbodyStart = html.indexOf('<tbody');
const tbodyEnd = html.indexOf('</tbody>');

console.log('tbody indices:', tbodyStart, tbodyEnd);

const tbodyHtml = html.substring(tbodyStart, tbodyEnd + 8);
const rowMatches = tbodyHtml.split(/<tr[\s>]/).slice(1);

console.log('Total table rows found in HTML:', rowMatches.length);

const parsedRows = [];

for (const row of rowMatches) {
  // extract cells
  const cells = row.split(/<\/td>/).map(c => {
    // strip tags
    return c.replace(/<[^>]+>/g, '').trim();
  });
  
  // also look for symbol link: href="/symbol/SYMBOL"
  const symMatch = row.match(/href="\/symbol\/([^"]+)"/);
  const symbol = symMatch ? symMatch[1].trim() : '';

  // Arabic name if present
  const arMatch = row.match(/dir="rtl"[^>]*>([^<]+)</);
  const nameAr = arMatch ? arMatch[1].trim() : '';

  // Company Name
  const nameEnMatch = row.match(/href="\/symbol\/[^"]+">([^<]+)<\/a>/);
  const nameEn = nameEnMatch ? nameEnMatch[1].trim() : '';

  // Cells typically:
  // 0: Position (#)
  // 1: Company (En + Ar)
  // 2: Symbol
  // 3: Sector
  // 4: Price
  // 5: Change
  // 6: Market Cap
  parsedRows.push({
    symbol,
    nameEn,
    nameAr,
    rawSector: cells[3] || '',
    rawPrice: cells[4] || '',
    rawChange: cells[5] || '',
    rawMarketCap: cells[6] || ''
  });
}

console.log('First 5 parsed:');
console.log(parsedRows.slice(0, 5));

console.log('USD stocks sample:');
console.log(parsedRows.filter(p => p.rawPrice.includes('USD') || ['FAITA', 'VLMR', 'SAIB', 'SPHT', 'EGBE', 'NAHO', 'CFGH', 'GTEX', 'TRTO', 'EGSA', 'MOIL'].includes(p.symbol)));

console.log('Stale / no price sample:');
console.log(parsedRows.filter(p => p.rawPrice.includes('—') || p.rawChange.includes('—')).slice(0, 5));

console.log('Unique Sectors in Starta:');
const sectors = [...new Set(parsedRows.map(p => p.rawSector))];
console.log(sectors);


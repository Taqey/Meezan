const https = require('https');
const fs = require('fs');

const options = {
  hostname: 'startamarkets.com',
  path: '/companies',
  headers: {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
  }
};

https.get(options, (res) => {
  let data = '';
  res.on('data', chunk => data += chunk);
  res.on('end', () => {
    fs.writeFileSync('tools/starta_page.html', data);
    console.log('Saved tools/starta_page.html. Size:', data.length);
  });
}).on('error', e => console.error(e));


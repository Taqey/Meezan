import fs from 'node:fs';
import path from 'node:path';

function kebab(s) {
  return s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
}

function extractStylesFromFile(htmlPath, cssPath, prefix) {
  if (!fs.existsSync(htmlPath)) return 0;
  let rawHtml = fs.readFileSync(htmlPath, 'utf8');
  let rawCss = fs.existsSync(cssPath) ? fs.readFileSync(cssPath, 'utf8') : '';

  const styles = new Map();
  let counter = 0;
  const EOL = '\r\n';

  let html = rawHtml.replace(/\sstyle="([^"]+)"/g, (full, cssText) => {
    // If it has angular interpolation like style="width: {{ x }}%", leave it as is or handle
    if (cssText.includes('{{')) return full;
    const norm = cssText.trim().replace(/\s+/g, ' ');
    if (!styles.has(norm)) {
      counter++;
      styles.set(norm, `${prefix}-${counter}`);
    }
    const clsName = styles.get(norm);
    return ` __INL_CLS__="${clsName}"`;
  });

  if (counter === 0) return 0;

  html = html.replace(/<([a-zA-Z0-9-]+)([^>]*?)__INL_CLS__="([^"]+)"([^>]*?)>/g, (m, tag, before, inlCls, after) => {
    const combined = before + after;
    if (/\bclass="([^"]*)"/.test(combined)) {
      return `<${tag}${combined.replace(/\bclass="([^"]*)"/, (_, c) => `class="${c} ${inlCls}"`)}>`;
    }
    return `<${tag}${before}class="${inlCls}"${after}>`;
  });

  const cssRules = [];
  for (const [cssText, clsName] of styles) {
    cssRules.push(`.${clsName} { ${cssText} }`);
  }

  const newCss = (rawCss.trim() ? rawCss.trim() + EOL + EOL : '') + cssRules.join(EOL) + EOL;

  fs.writeFileSync(htmlPath, html, 'utf8');
  fs.writeFileSync(cssPath, newCss, 'utf8');
  console.log(`Extracted ${counter} inline styles from ${htmlPath}`);
  return counter;
}

// 1. loading-skeletons
extractStylesFromFile(
  'frontend/src/app/shared/components/loading-skeletons/loading-skeletons.component.html',
  'frontend/src/app/shared/components/loading-skeletons/loading-skeletons.component.css',
  'skel-inl'
);

// 2. fund-detail
extractStylesFromFile(
  'frontend/src/app/features/funds/fund-detail/fund-detail.component.html',
  'frontend/src/app/features/funds/fund-detail/fund-detail.component.css',
  'fdetail-inl'
);

// 3. fund-manager-detail
extractStylesFromFile(
  'frontend/src/app/features/funds/fund-manager-detail/fund-manager-detail.component.html',
  'frontend/src/app/features/funds/fund-manager-detail/fund-manager-detail.component.css',
  'fmgr-inl'
);

// 4. funds-list
extractStylesFromFile(
  'frontend/src/app/features/funds/funds-list/funds-list.component.html',
  'frontend/src/app/features/funds/funds-list/funds-list.component.css',
  'flist-inl'
);

// 5. portfolio
extractStylesFromFile(
  'frontend/src/app/features/portfolio/portfolio/portfolio.component.html',
  'frontend/src/app/features/portfolio/portfolio/portfolio.component.css',
  'port-inl'
);

// 6. favorites
extractStylesFromFile(
  'frontend/src/app/features/favorites/favorites/favorites.component.html',
  'frontend/src/app/features/favorites/favorites/favorites.component.css',
  'fav-inl'
);

// 7. mz-listing-card
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-listing-card/mz-listing-card.component.html',
  'frontend/src/app/shared/components/ui/mz-listing-card/mz-listing-card.component.css',
  'lcard-inl'
);

// 8. mz-fair-value-card
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-fair-value-card/mz-fair-value-card.component.html',
  'frontend/src/app/shared/components/ui/mz-fair-value-card/mz-fair-value-card.component.css',
  'fvcard-inl'
);

// 9. mz-details-header-card
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-details-header-card/mz-details-header-card.component.html',
  'frontend/src/app/shared/components/ui/mz-details-header-card/mz-details-header-card.component.css',
  'dhead-inl'
);

// 10. mz-filters-bar
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-filters-bar/mz-filters-bar.component.html',
  'frontend/src/app/shared/components/ui/mz-filters-bar/mz-filters-bar.component.css',
  'fbar-inl'
);

// 11. mz-performance-chart
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-performance-chart/mz-performance-chart.component.html',
  'frontend/src/app/shared/components/ui/mz-performance-chart/mz-performance-chart.component.css',
  'pchart-inl'
);

// 12. mz-stat-card
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-stat-card/mz-stat-card.component.html',
  'frontend/src/app/shared/components/ui/mz-stat-card/mz-stat-card.component.css',
  'scard-inl'
);

// 13. mz-support-resistance-card
extractStylesFromFile(
  'frontend/src/app/shared/components/ui/mz-support-resistance-card/mz-support-resistance-card.component.html',
  'frontend/src/app/shared/components/ui/mz-support-resistance-card/mz-support-resistance-card.component.css',
  'srcard-inl'
);

// 14. admin-classification & admin-funds top level coordinators
extractStylesFromFile(
  'frontend/src/app/features/admin/admin-classification/admin-classification.component.html',
  'frontend/src/app/features/admin/admin-classification/admin-classification.component.css',
  'aclass-inl'
);

extractStylesFromFile(
  'frontend/src/app/features/admin/admin-funds/admin-funds.component.html',
  'frontend/src/app/features/admin/admin-funds/admin-funds.component.css',
  'afunds-inl'
);

console.log('Finished migrating all inline styles to component css!');


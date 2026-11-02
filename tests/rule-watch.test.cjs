const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const { test } = require('node:test');

const html = readFileSync(join(__dirname, '..', 'index.html'), 'utf8');
const pagePath = html.match(/src="([^\"]*\/app\/page-[^\"]+\.js)"/)[1];
const page = readFileSync(join(__dirname, '..', pagePath), 'utf8');

test('server HTML and active page show an unset replacement date', () => {
  for (const content of [html, page]) {
    assert.match(content, /EPC cutoff deferred: replacement date not set/);
    assert.match(content, /February 2027/);
    assert.match(content, /epc-defers-setting-new-end-date-unstructured-address-format-until-february-2027/);
    assert.doesNotMatch(content, /Current EPC planning basis: 15 November 2026|Time to planning date|74 days|EPC will review its position on 9 September/);
  }
  assert.doesNotMatch(page, /new Date\("2026-11-15/);
  assert.match(page, /No replacement EPC cutoff is set/);
  assert.match(page, /these results are not current scheme rejections/);
});

test('exports use the corrected regulatory status', () => {
  assert.match(page, /EPC cutoff deferred; replacement date not set \(checked 2 November 2026\)/);
  assert.doesNotMatch(page, /EPC timeline remains 15 November 2026/);
});

const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

test('startup restores saved records before writing the workspace', () => {
  const html = readFileSync(join(__dirname, '..', 'index.html'), 'utf8');
  const path = html.match(/src="([^\"]*\/app\/page-[^\"]+\.js)"/)[1];
  const page = readFileSync(join(__dirname, '..', path), 'utf8');
  const initialization = page.match(/function ez\(\) \{([\s\S]*?)\n        let eI =/)[1];
  const saved = JSON.stringify([{ id: 'saved-review', status: 'Approved' }]);
  const storage = new Map([['paydata-records-v1', saved]]);
  const states = [], frames = [], previousEffects = [];
  let stateIndex = 0, effectIndex = 0, effects = [];
  const context = { ej: [{ id: 'seed', status: 'Open' }],
    localStorage: { getItem: key => storage.get(key) ?? null, setItem: (key, value) => storage.set(key, value) },
    window: { requestAnimationFrame: callback => (frames.push(callback), frames.length), cancelAnimationFrame() {} },
    a: {
      useState(value) { const i = stateIndex++; if (!(i in states)) states[i] = value; return [states[i], next => { states[i] = typeof next === 'function' ? next(states[i]) : next; }]; },
      useRef: value => ({ current: value }),
      useEffect(callback, deps) { const i = effectIndex++; const prior = previousEffects[i]; if (!prior || deps.some((value, j) => !Object.is(value, prior[j]))) effects.push(callback); previousEffects[i] = deps; },
    },
  };
  vm.createContext(context);
  function render() { stateIndex = 0; effectIndex = 0; effects = []; vm.runInContext(`(() => {${initialization}})()`, context); for (const effect of effects) effect(); }
  render();
  assert.equal(storage.get('paydata-records-v1'), saved, 'Do not overwrite saved reviews before restoration');
  for (const frame of frames.splice(0)) frame();
  render();
  assert.equal(storage.get('paydata-records-v1'), saved);
  assert.equal(states[1][0].id, 'saved-review');
});

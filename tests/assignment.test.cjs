"use strict";

const { test } = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const vm = require("node:vm");
const { webcrypto } = require("node:crypto");

function createApp() {
  const controls = new Map();
  const control = selector => {
    if (!controls.has(selector)) controls.set(selector, {
      value: "", textContent: "", disabled: false, style: {},
      addEventListener() {}, focus() {}, classList: { toggle() {} }
    });
    return controls.get(selector);
  };
  let stored = null;
  let failSave = false;
  const context = vm.createContext({
    structuredClone, crypto: webcrypto, console, Uint32Array,
    navigator: {}, window: { addEventListener() {} },
    document: {
      querySelector: control,
      querySelectorAll: () => [...controls.values()],
      body: { classList: { toggle() {} } }
    },
    localStorage: {
      getItem: () => stored === null ? null : JSON.stringify(stored),
      setItem: (_key, value) => {
        if (failSave) throw new Error("Storage full");
        stored = JSON.parse(value);
      }
    },
    matchMedia: () => ({ matches: false }), setTimeout
  });
  // Skip only the initial DOM render; run the production assignment handlers unchanged.
  const source = readFileSync(require.resolve("../web/app.js"), "utf8");
  vm.runInContext(source.replace("\nrender();\n", "\n"), context);
  vm.runInContext("render = () => updateActionButtons();", context);
  return {
    run: code => vm.runInContext(code, context),
    state: () => JSON.parse(vm.runInContext("JSON.stringify(state)", context)),
    stored: () => stored,
    failSave: () => { failSave = true; },
    controls
  };
}

function seed(app, count = 16) {
  app.run(`state = structuredClone(DEFAULT_STATE);
    state.animationDelayMs = 0;
    state.sharingTitle = "나눔";
    state.teamNames = ["새싹", "열매", "믿음", "소망"];
    state.participants = Array.from({ length: ${count} }, (_, index) =>
      ({ id: String(index), name: "참가자" + index, teamNumber: index < 8 ? 1 : null }));
    state.pendingOrder = state.participants.filter(person => person.teamNumber === null).map(person => person.id);`);
}

test("전체 재배정은 대기자를 포함하고 명단과 설정을 보존하며 균형 배정한다", async () => {
  const app = createApp();
  seed(app);
  const before = app.state();
  await app.run("reshuffleTeams()");
  const after = app.state();
  assert.deepEqual(after.pendingOrder, []);
  assert.deepEqual(after.participants.map(({ id, name }) => ({ id, name })),
    before.participants.map(({ id, name }) => ({ id, name })));
  for (const key of ["sharingTitle", "teamNames", "teamWeights", "animationDelayMs", "version"])
    assert.deepEqual(after[key], before[key]);
  assert.deepEqual([1, 2, 3, 4].map(team => after.participants.filter(person => person.teamNumber === team).length), [4, 4, 4, 4]);
  assert.deepEqual(app.stored(), after);
  assert.deepEqual(JSON.parse(app.run("JSON.stringify(loadState())")), after);
  assert.equal(app.run("isBusy"), false);
});

test("재배정도 가중치 규칙을 사용하고 매번 새로운 순서로 섞는다", async () => {
  const app = createApp();
  seed(app, 20);
  app.run("state.teamWeights = [-1, 0, 1, 0]; orders = []; originalRandomIndex = randomIndex; randomIndex = length => { orders.push(length); return originalRandomIndex(length); };");
  await app.run("reshuffleTeams()");
  const after = app.state();
  const scores = [1, 2, 3, 4].map((team, index) => after.participants.filter(person => person.teamNumber === team).length + after.teamWeights[index]);
  assert.deepEqual(scores, [5, 5, 5, 5]);
  assert.deepEqual(JSON.parse(app.run("JSON.stringify(orders.slice(0, 19))")), Array.from({ length: 19 }, (_, index) => 20 - index));
  app.run("orders = []");
  await app.run("reshuffleTeams()");
  assert.equal(app.run("orders[0]"), 20);
});

test("추가 배정은 기존 참가자의 조를 유지한다", async () => {
  const app = createApp();
  seed(app);
  await app.run("assignPending()");
  assert.ok(app.state().participants.slice(0, 8).every(person => person.teamNumber === 1));
  assert.ok(app.state().participants.every(person => person.teamNumber !== null));
});

test("저장 실패 시 기존 배정과 대기 순서를 복원한다", async () => {
  const app = createApp();
  seed(app);
  const before = app.state();
  app.failSave();
  await app.run("reshuffleTeams()");
  assert.deepEqual(app.state(), before);
  assert.equal(app.run("isBusy"), false);
  assert.match(app.controls.get("#validationMessage").textContent, /저장하지 못했습니다/);
});

test("배정 전에는 재배정이 비활성이고 실행 중 재진입을 막는다", async () => {
  const app = createApp();
  seed(app);
  app.run("state.participants.forEach(person => person.teamNumber = null); updateActionButtons()");
  assert.equal(app.controls.get("#reshuffleButton").disabled, true);
  const before = app.state();
  await app.run("reshuffleTeams()");
  assert.deepEqual(app.state(), before);
  seed(app);
  app.run("isBusy = true; updateActionButtons()");
  assert.equal(app.controls.get("#reshuffleButton").disabled, true);
  const busyState = app.state();
  await app.run("reshuffleTeams()");
  assert.deepEqual(app.state(), busyState);
});

test("공개 전에 결과가 저장되고 중복 실행 없이 기존 공개 연출을 마친다", async () => {
  const app = createApp();
  seed(app);
  app.run(`state.animationDelayMs = 200; steps = 0; delay = async () => {
    if (!isBusy || revealedIds === null) throw new Error('Invalid reveal state');
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (saved.participants.some(person => person.teamNumber === null)) throw new Error('Result was not saved before reveal');
    steps++; await reshuffleTeams(); await assignPending();
  };`);
  await app.run("reshuffleTeams()");
  assert.equal(app.run("steps"), 16);
  assert.ok(app.stored().participants.every(person => person.teamNumber !== null));
  assert.equal(app.run("revealedIds"), null);
  assert.equal(app.run("isBusy"), false);
});

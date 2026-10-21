"use strict";

const STORAGE_KEY = "random-team-draw.session.v1";
const DEFAULT_STATE = Object.freeze({
  version: 1,
  participants: [],
  pendingOrder: [],
  lastAssignedId: null,
  teamNames: ["1조", "2조", "3조", "4조"],
  teamWeights: [0, 0, 0, 0]
});
const TEAM_COLORS = [
  ["#2563eb", "#bfdbfe"],
  ["#059669", "#a7f3d0"],
  ["#9333ea", "#e9d5ff"],
  ["#d97706", "#fed7aa"]
];

let state = loadState();
let settingsDraft = null;
let deferredInstallPrompt = null;
let isBusy = false;
let revealedIds = null;

const ui = {
  form: document.querySelector("#participantForm"),
  nameInput: document.querySelector("#nameInput"),
  validation: document.querySelector("#validationMessage"),
  participantList: document.querySelector("#participantList"),
  participantCount: document.querySelector("#participantCount"),
  assignButton: document.querySelector("#assignButton"),
  totalCount: document.querySelector("#totalCount"),
  lastAssigned: document.querySelector("#lastAssigned"),
  teamGrid: document.querySelector("#teamGrid"),
  participantTemplate: document.querySelector("#participantTemplate"),
  installButton: document.querySelector("#installButton"),
  settingsButton: document.querySelector("#settingsButton"),
  resetButton: document.querySelector("#resetButton"),
  settingsDialog: document.querySelector("#settingsDialog"),
  settingsForm: document.querySelector("#settingsForm"),
  settingsRows: document.querySelector("#settingsRows")
};

ui.form.addEventListener("submit", addParticipant);
ui.assignButton.addEventListener("click", assignPending);
ui.installButton.addEventListener("click", installApp);
ui.settingsButton.addEventListener("click", openSettings);
ui.resetButton.addEventListener("click", resetAll);
ui.settingsForm.addEventListener("submit", saveSettings);
ui.settingsDialog.addEventListener("click", closeOnBackdrop);
window.addEventListener("beforeinstallprompt", prepareInstall);
window.addEventListener("appinstalled", finishInstall);

if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("./service-worker.js").catch(error => {
      console.error("Service Worker 등록 실패:", error);
    });
  });
}

render();

function prepareInstall(event) {
  event.preventDefault();
  deferredInstallPrompt = event;
  ui.installButton.hidden = false;
}

async function installApp() {
  if (!deferredInstallPrompt) return;
  await deferredInstallPrompt.prompt();
  await deferredInstallPrompt.userChoice;
  deferredInstallPrompt = null;
  ui.installButton.hidden = true;
}

function finishInstall() {
  deferredInstallPrompt = null;
  ui.installButton.hidden = true;
}

function loadState() {
  try {
    const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (!parsed || parsed.version !== 1 || !Array.isArray(parsed.participants)) return structuredClone(DEFAULT_STATE);
    return {
      version: 1,
      participants: parsed.participants.filter(isValidParticipant),
      pendingOrder: normalizePendingOrder(parsed.pendingOrder, parsed.participants),
      lastAssignedId: typeof parsed.lastAssignedId === "string" ? parsed.lastAssignedId : null,
      teamNames: normalizeTeamNames(parsed.teamNames),
      teamWeights: normalizeWeights(parsed.teamWeights)
    };
  } catch {
    return structuredClone(DEFAULT_STATE);
  }
}

function isValidParticipant(participant) {
  return participant && typeof participant.id === "string" && typeof participant.name === "string" &&
    (participant.teamNumber === null || (Number.isInteger(participant.teamNumber) && participant.teamNumber >= 1 && participant.teamNumber <= 4));
}

function normalizePendingOrder(order, participants) {
  const pendingIds = new Set((Array.isArray(participants) ? participants : [])
    .filter(isValidParticipant).filter(person => person.teamNumber === null).map(person => person.id));
  const normalized = Array.isArray(order) ? order.filter(id => pendingIds.delete(id)) : [];
  return [...normalized, ...pendingIds];
}

function normalizeTeamNames(names) {
  return DEFAULT_STATE.teamNames.map((fallback, index) => {
    const name = Array.isArray(names) && typeof names[index] === "string" ? names[index].trim() : "";
    return name && name.length <= 20 ? name : fallback;
  });
}

function normalizeWeights(weights) {
  return [0, 1, 2, 3].map(index => Math.max(-10, Math.min(10,
    Number.isInteger(weights?.[index]) ? weights[index] : 0)));
}

function saveState() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    return true;
  } catch {
    document.querySelector("#saveStatus").textContent = "저장 실패";
    document.querySelector(".save-dot").style.background = "#ef4444";
    return false;
  }
}

function addParticipant(event) {
  event.preventDefault();
  if (isBusy) return;
  const name = validateName(ui.nameInput.value);
  if (!name) return;

  const participant = { id: crypto.randomUUID(), name, teamNumber: null };
  state.participants.push(participant);
  state.pendingOrder.splice(randomIndex(state.pendingOrder.length + 1), 0, participant.id);
  ui.nameInput.value = "";
  ui.validation.textContent = "";
  commit();
  ui.nameInput.focus();
}

function validateName(value, editingId = null) {
  const name = value.trim();
  if (!name) return showValidation("이름을 입력해 주세요.");
  if (name.length > 30) return showValidation("이름은 30자까지 입력할 수 있습니다.");
  const duplicate = state.participants.some(person => person.id !== editingId && person.name.toLocaleLowerCase() === name.toLocaleLowerCase());
  if (duplicate) return showValidation("이미 등록된 이름입니다.");
  return name;
}

function showValidation(message) {
  ui.validation.textContent = message;
  return null;
}

function chooseTeam(participants = state.participants) {
  const scores = [1, 2, 3, 4].map((teamNumber, index) =>
    participants.filter(person => person.teamNumber === teamNumber).length + state.teamWeights[index]);
  const minimum = Math.min(...scores);
  const candidates = scores.map((score, index) => score === minimum ? index + 1 : null).filter(Boolean);
  return candidates[randomIndex(candidates.length)];
}

async function assignPending() {
  if (isBusy || !state.pendingOrder.length) return;
  setBusy(true);
  const previousState = structuredClone(state);
  const assignmentOrder = [...state.pendingOrder];
  const assigned = state.participants.filter(person => person.teamNumber !== null);
  const previouslyAssignedIds = assigned.map(person => person.id);
  const results = [];
  assignmentOrder.forEach(id => {
    const person = state.participants.find(candidate => candidate.id === id);
    if (!person) return;
    person.teamNumber = chooseTeam(assigned);
    assigned.push(person);
    results.push(person.id);
  });
  state.pendingOrder = [];
  state.lastAssignedId = results.at(-1) ?? state.lastAssignedId;
  if (!saveState()) {
    state = previousState;
    render();
    setBusy(false);
    showValidation("조 편성 결과를 저장하지 못했습니다. 브라우저 저장 공간을 확인해 주세요.");
    return;
  }

  if (matchMedia("(prefers-reduced-motion: reduce)").matches) {
    render();
  } else {
    revealedIds = new Set(previouslyAssignedIds);
    for (const id of results) {
      revealedIds.add(id);
      state.lastAssignedId = id;
      render();
      await animateAssignment(id);
      await delay(assignmentDelay(results.length));
    }
    state.lastAssignedId = results.at(-1) ?? null;
    saveState();
    revealedIds = null;
    render();
  }
  setBusy(false);
  ui.nameInput.focus();
}

function assignmentDelay(count) { return count <= 12 ? 80 : count <= 30 ? 45 : 15; }
function delay(milliseconds) { return new Promise(resolve => setTimeout(resolve, milliseconds)); }

function setBusy(busy) {
  isBusy = busy;
  document.querySelectorAll("button, input").forEach(control => control.disabled = busy);
  if (!busy) updateActionButtons();
  document.body.classList.toggle("busy", busy);
}

function randomIndex(length) {
  const max = Math.floor(0x100000000 / length) * length;
  const buffer = new Uint32Array(1);
  do crypto.getRandomValues(buffer); while (buffer[0] >= max);
  return buffer[0] % length;
}

function commit(animateParticipantId = null) {
  saveState();
  render();
  if (animateParticipantId) {
    requestAnimationFrame(() => animateAssignment(animateParticipantId));
  }
}

function animateAssignment(participantId) {
  if (matchMedia("(prefers-reduced-motion: reduce)").matches) return Promise.resolve();

  const person = state.participants.find(candidate => candidate.id === participantId);
  const target = ui.teamGrid.querySelector(`.team-member[data-participant-id="${CSS.escape(participantId)}"]`);
  if (!person || !target || ui.lastAssigned.hidden) return Promise.resolve();

  const sourceRect = ui.lastAssigned.getBoundingClientRect();
  const targetRect = target.getBoundingClientRect();
  if (!sourceRect.width || !targetRect.width) return Promise.resolve();

  const flight = document.createElement("div");
  flight.className = "assignment-flight";
  flight.style.setProperty("--flight-team-color", TEAM_COLORS[person.teamNumber - 1][0]);
  flight.style.left = `${sourceRect.left}px`;
  flight.style.top = `${sourceRect.top}px`;
  flight.style.width = `${sourceRect.width}px`;
  flight.style.height = `${sourceRect.height}px`;

  const label = document.createElement("small");
  label.textContent = "배정 완료";
  const name = document.createElement("strong");
  name.textContent = person.name;
  const team = document.createElement("span");
  team.textContent = state.teamNames[person.teamNumber - 1];
  flight.append(label, name, team);
  document.body.append(flight);

  target.classList.add("receiving");
  const translateX = targetRect.left - sourceRect.left;
  const translateY = targetRect.top - sourceRect.top;
  const scaleX = targetRect.width / sourceRect.width;
  const scaleY = targetRect.height / sourceRect.height;

  const animation = flight.animate([
    { transform: "translate(0, 0) scale(1)", opacity: 1, offset: 0 },
    { transform: "translate(0, -8px) scale(1.015)", opacity: 1, offset: .16 },
    { transform: `translate(${translateX}px, ${translateY}px) scale(${scaleX}, ${scaleY})`, opacity: .92, offset: 1 }
  ], {
    duration: 270,
    easing: "cubic-bezier(.22, .82, .28, 1)",
    fill: "forwards"
  });

  return animation.finished
    .catch(() => undefined)
    .finally(() => {
      flight.remove();
      target.classList.remove("receiving");
      target.animate([
        { transform: "scale(.96)", boxShadow: "0 0 0 0 rgba(37, 99, 235, .35)" },
        { transform: "scale(1.035)", boxShadow: "0 0 0 7px rgba(37, 99, 235, 0)" },
        { transform: "scale(1)", boxShadow: "none" }
      ], { duration: 360, easing: "ease-out" });
    });
}

function render() {
  const visibleParticipants = [...state.participants].sort((a, b) => {
    if (a.teamNumber === null && b.teamNumber !== null) return -1;
    if (a.teamNumber !== null && b.teamNumber === null) return 1;
    return 0;
  });
  ui.participantList.replaceChildren(...visibleParticipants.map(createParticipantRow));
  ui.participantCount.textContent = `${state.participants.length}명`;
  const assignedCount = state.participants.filter(person => person.teamNumber !== null).length;
  ui.totalCount.textContent = `배정 ${assignedCount}명`;
  updateActionButtons();
  renderLastAssigned();
  renderTeams();
}

function createParticipantRow(person) {
  const row = ui.participantTemplate.content.firstElementChild.cloneNode(true);
  row.dataset.id = person.id;
  row.classList.toggle("highlight", person.id === state.lastAssignedId);
  row.querySelector(".participant-name").textContent = person.name;
  const visuallyAssigned = isVisuallyAssigned(person);
  row.classList.toggle("pending", !visuallyAssigned);
  row.querySelector(".participant-team").textContent = visuallyAssigned
    ? state.teamNames[person.teamNumber - 1]
    : person.teamNumber === null ? "배정 대기" : "결과 공개 대기";
  row.querySelector(".edit").addEventListener("click", () => beginEdit(row, person));
  row.querySelector(".delete").addEventListener("click", () => deleteParticipant(person.id));
  row.querySelector(".save").addEventListener("click", () => saveEdit(row, person));
  row.querySelector(".cancel").addEventListener("click", () => cancelEdit(row));
  row.querySelector(".edit-form").addEventListener("submit", event => {
    event.preventDefault();
    saveEdit(row, person);
  });
  return row;
}

function beginEdit(row, person) {
  document.querySelectorAll(".participant-row.editing").forEach(cancelEdit);
  row.classList.add("editing");
  row.querySelector(".participant-summary").hidden = true;
  row.querySelector(".normal-actions").hidden = true;
  row.querySelector(".edit-form").hidden = false;
  row.querySelector(".edit-actions").hidden = false;
  const input = row.querySelector(".edit-input");
  input.value = person.name;
  input.focus();
  input.select();
}

function cancelEdit(row) {
  row.classList.remove("editing");
  row.querySelector(".participant-summary").hidden = false;
  row.querySelector(".normal-actions").hidden = false;
  row.querySelector(".edit-form").hidden = true;
  row.querySelector(".edit-actions").hidden = true;
}

function saveEdit(row, person) {
  const name = validateName(row.querySelector(".edit-input").value, person.id);
  if (!name) return;
  person.name = name;
  ui.validation.textContent = "";
  commit();
}

function deleteParticipant(id) {
  if (isBusy) return;
  state.participants = state.participants.filter(person => person.id !== id);
  state.pendingOrder = state.pendingOrder.filter(candidate => candidate !== id);
  if (state.lastAssignedId === id) state.lastAssignedId = null;
  ui.validation.textContent = "";
  commit();
}

function updateActionButtons() {
  const pendingCount = state.pendingOrder.length;
  const hasAssigned = state.participants.some(person => person.teamNumber !== null);
  ui.assignButton.disabled = isBusy || pendingCount === 0;
  ui.assignButton.textContent = pendingCount === 0
    ? "대기 참가자 없음"
    : hasAssigned ? `대기 ${pendingCount}명 추가 배정` : `${pendingCount}명 조 편성 시작`;
}

function renderLastAssigned() {
  const person = state.participants.find(candidate => candidate.id === state.lastAssignedId);
  if (!person) {
    ui.lastAssigned.hidden = true;
    ui.lastAssigned.replaceChildren();
    return;
  }
  const label = document.createElement("small");
  label.textContent = "방금 배정";
  const name = document.createElement("strong");
  name.textContent = person.name;
  const team = document.createElement("span");
  team.textContent = state.teamNames[person.teamNumber - 1];
  ui.lastAssigned.replaceChildren(label, name, team);
  ui.lastAssigned.hidden = false;
}

function renderTeams() {
  const cards = [1, 2, 3, 4].map((teamNumber, index) => {
    const card = document.createElement("section");
    card.className = "team-card";
    card.style.setProperty("--team", TEAM_COLORS[index][0]);
    card.style.setProperty("--team-light", TEAM_COLORS[index][1]);
    const members = state.participants.filter(person => person.teamNumber === teamNumber && isVisuallyAssigned(person));
    const title = document.createElement("h3");
    title.textContent = state.teamNames[index];
    const count = document.createElement("p");
    count.className = "team-count";
    count.textContent = `${members.length}명`;
    const list = document.createElement("div");
    list.className = "team-members";
    if (!members.length) {
      const empty = document.createElement("p");
      empty.className = "empty-team";
      empty.textContent = "배정 대기";
      list.append(empty);
    } else {
      members.forEach(person => {
        const member = document.createElement("div");
        member.className = "team-member";
        member.dataset.participantId = person.id;
        member.classList.toggle("highlight", person.id === state.lastAssignedId);
        member.textContent = person.name;
        member.title = person.name;
        list.append(member);
      });
    }
    card.append(title, count, list);
    return card;
  });
  ui.teamGrid.replaceChildren(...cards);
}

function isVisuallyAssigned(person) {
  return person.teamNumber !== null && (revealedIds === null || revealedIds.has(person.id));
}

function openSettings() {
  settingsDraft = { teamNames: [...state.teamNames], teamWeights: [...state.teamWeights] };
  renderSettings();
  ui.settingsDialog.showModal();
}

function renderSettings() {
  const rows = settingsDraft.teamNames.map((teamName, index) => {
    const row = document.createElement("div");
    row.className = "settings-row";
    const label = document.createElement("label");
    label.textContent = `${index + 1}조 이름`;
    const input = document.createElement("input");
    input.maxLength = 20;
    input.value = teamName;
    input.addEventListener("input", () => settingsDraft.teamNames[index] = input.value);
    label.append(input);
    const control = document.createElement("div");
    control.className = "weight-control";
    const decrease = weightButton("−", () => updateDraftWeight(index, -1));
    const value = document.createElement("span");
    value.className = "weight-value";
    value.textContent = formatWeight(settingsDraft.teamWeights[index]);
    const increase = weightButton("＋", () => updateDraftWeight(index, 1));
    control.append(decrease, value, increase);
    row.append(label, control);
    return row;
  });
  ui.settingsRows.replaceChildren(...rows);
}

function weightButton(label, action) {
  const button = document.createElement("button");
  button.type = "button";
  button.textContent = label;
  button.addEventListener("click", action);
  return button;
}

function updateDraftWeight(index, delta) {
  settingsDraft.teamWeights[index] = Math.max(-10, Math.min(10, settingsDraft.teamWeights[index] + delta));
  renderSettings();
}

function formatWeight(weight) { return weight > 0 ? `+${weight}` : String(weight); }

function saveSettings(event) {
  if (event.submitter?.value !== "default") return;
  event.preventDefault();
  const names = settingsDraft.teamNames.map(name => name.trim());
  if (names.some(name => !name || name.length > 20)) {
    alert("조 이름은 공백이 아닌 20자 이하로 입력해 주세요.");
    return;
  }
  state.teamNames = names;
  state.teamWeights = [...settingsDraft.teamWeights];
  commit();
  ui.settingsDialog.close();
  ui.nameInput.focus();
}

function closeOnBackdrop(event) {
  if (event.target === ui.settingsDialog) ui.settingsDialog.close();
}

function resetAll() {
  if (isBusy) return;
  if (!confirm("모든 참가자와 조 편성 결과를 초기화할까요?\n조 이름과 가중치 설정은 유지됩니다.")) return;
  state.participants = [];
  state.pendingOrder = [];
  state.lastAssignedId = null;
  ui.validation.textContent = "";
  commit();
  ui.nameInput.focus();
}

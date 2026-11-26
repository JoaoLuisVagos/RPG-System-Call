const API_URL = "http://localhost:5080/api";
const state = { data: null, sides: 20, zoomed: false, initiative: [], toastTimer: null };
const $ = selector => document.querySelector(selector);
const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, character => ({
  "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
})[character]);
const initials = name => name.trim().split(/\s+/).slice(0, 2).map(part => part[0] || "").join("").toUpperCase();
const activeMap = () => state.data?.maps.find(map => map.id === state.data.activeMapId);
const validImageData = value => typeof value === "string" && value.startsWith("data:image/") && value.length <= 10_000_000;

async function api(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: { "Content-Type": "application/json", ...options.headers }
  });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || "Não foi possível concluir essa ação.");
  }
  return response.status === 204 ? null : response.json();
}

function notify(message) {
  const toast = $("#toast");
  toast.textContent = message;
  toast.classList.add("visible");
  clearTimeout(state.toastTimer);
  state.toastTimer = setTimeout(() => toast.classList.remove("visible"), 3000);
}

async function loadState() {
  try {
    state.data = await api("/state");
    render();
  } catch {
    notify("Não foi possível conectar. Inicie a API .NET e tente novamente.");
  }
}

function render() {
  renderCampaign();
  renderMaps();
  renderBoard();
  renderPlayers();
  renderSheets();
  renderInitiative();
}

function renderCampaign() {
  const name = state.data.campaignName || "Nova campanha";
  $("#campaign-name").textContent = name;
  $("#top-campaign-name").textContent = name;
  $("#character-count").textContent = state.data.characters.length;
  $("#player-count").textContent = `${state.data.characters.length} ${state.data.characters.length === 1 ? "personagem" : "personagens"}`;
}

function renderMaps() {
  const mapList = $("#map-list");
  mapList.innerHTML = state.data.maps.length ? state.data.maps.map(map => `
    <button class="map-entry ${map.id === state.data.activeMapId ? "active" : ""}" type="button" data-map-id="${escapeHtml(map.id)}" aria-label="Abrir mapa ${escapeHtml(map.name)}">
      <span class="map-thumbnail"></span>
      <span class="map-entry-name">${escapeHtml(map.name)}</span>${map.id === state.data.activeMapId ? '<span class="map-current">•</span>' : ""}
    </button>`).join("") : '<p class="empty-maps">Seus mapas aparecerão aqui.</p>';
  mapList.querySelectorAll("[data-map-id]").forEach(button => button.addEventListener("click", async () => {
    try {
      await api(`/maps/${button.dataset.mapId}/active`, { method: "PUT" });
      state.data.activeMapId = button.dataset.mapId;
      render();
    } catch (error) {
      notify(error.message);
    }
  }));
  mapList.querySelectorAll("[data-map-id]").forEach(button => {
    const map = state.data.maps.find(item => item.id === button.dataset.mapId);
    if (!validImageData(map?.imageData)) return;
    const image = document.createElement("img");
    image.src = map.imageData;
    image.alt = "";
    button.querySelector(".map-thumbnail").append(image);
  });
}

function renderBoard() {
  const map = activeMap();
  const imageData = validImageData(map?.imageData) ? map.imageData : null;
  $("#map-heading").textContent = map?.name || "Sua mesa";
  $("#map-art").classList.toggle("has-image", Boolean(imageData));
  $("#map-art").textContent = "";
  if (imageData) {
    const image = document.createElement("img");
    image.src = imageData;
    image.alt = `Mapa ${map.name}`;
    $("#map-art").append(image);
  }
  $("#board-empty").hidden = Boolean(map);
  $("#map-grid").classList.toggle("hidden-grid", !map);
  const tokens = map ? state.data.tokens.filter(token => token.mapId === map.id) : [];
  $("#token-layer").innerHTML = tokens.map(token => `
    <button class="token" type="button" data-token-id="${escapeHtml(token.id)}" aria-label="Mover ${escapeHtml(token.name)}" style="left:${token.x}%;top:${token.y}%;--token-color:${escapeHtml(token.color)}">
      <span class="token-orb">${escapeHtml(initials(token.name))}</span><span class="token-name">${escapeHtml(token.name)}</span>
    </button>`).join("");
  $("#token-layer").querySelectorAll(".token").forEach(bindTokenDrag);
}

function renderPlayers() {
  $("#player-list").innerHTML = state.data.characters.map(character => {
    const percentage = Math.min(100, Math.round(character.hitPoints / character.maxHitPoints * 100));
    return `<button class="player-card" type="button" data-edit-id="${escapeHtml(character.id)}" aria-label="Editar ${escapeHtml(character.name)}">
      <span class="player-portrait">${escapeHtml(initials(character.name))}</span>
      <span class="player-copy"><strong>${escapeHtml(character.name)}</strong><small>${escapeHtml(character.role || "Aventureiro")} · Nv. ${character.level}</small></span>
      <span class="player-meta"><span class="hp-label">${character.hitPoints}/${character.maxHitPoints} PV</span><span class="hp-track"><span style="width:${percentage}%"></span></span><span class="player-level">CA ${character.armorClass}</span></span>
    </button>`;
  }).join("");
  $("#player-list").querySelectorAll("[data-edit-id]").forEach(button => button.addEventListener("click", () => openCharacterForm(button.dataset.editId)));
}

function renderSheets() {
  const container = $("#character-grid");
  if (!state.data.characters.length) {
    container.innerHTML = '<div class="sheets-empty"><span>✧</span><strong>O primeiro capítulo começa com um personagem.</strong><p>Crie uma ficha ou importe uma que você já tenha.</p><button class="primary-button" data-create-character type="button">Criar personagem</button></div>';
    container.querySelector("[data-create-character]").addEventListener("click", () => openCharacterForm());
    return;
  }

  container.innerHTML = state.data.characters.map(character => {
    const percentage = Math.min(100, Math.round(character.hitPoints / character.maxHitPoints * 100));
    const attributes = Object.entries(character.attributes || {}).slice(0, 6);
    return `<article class="character-sheet-card">
      <div class="sheet-banner"><span class="sheet-role">${escapeHtml(character.role || "Aventureiro")}</span><span class="sheet-level">Nv. ${character.level}</span></div>
      <div class="sheet-card-body">
        <div class="sheet-name-row"><h2>${escapeHtml(character.name)}</h2><span class="sheet-actions"><button class="icon-button" data-edit-id="${escapeHtml(character.id)}" type="button" aria-label="Editar ficha de ${escapeHtml(character.name)}">✎</button><button class="icon-button" data-delete-id="${escapeHtml(character.id)}" type="button" aria-label="Excluir ficha de ${escapeHtml(character.name)}">×</button></span></div>
        <p class="sheet-species">${escapeHtml(character.species || "Espécie não definida")}</p>
        <div class="sheet-stats"><div class="sheet-stat"><span>Vida atual</span><strong>${character.hitPoints} PV</strong></div><div class="sheet-stat"><span>Vida máxima</span><strong>${character.maxHitPoints} PV</strong></div><div class="sheet-stat"><span>Armadura</span><strong>${character.armorClass} CA</strong></div></div>
        <div class="sheet-hp"><div class="sheet-hp-line"><span>Pontos de vida</span><span>${character.hitPoints} / ${character.maxHitPoints}</span></div><div class="hp-track"><span style="width:${percentage}%"></span></div></div>
        <p class="sheet-notes">${escapeHtml(character.notes || "Esta história ainda está sendo escrita.")}</p>
        <div class="attributes-row">${attributes.map(([name, value]) => `<div class="attribute"><span>${escapeHtml(name.slice(0, 3))}</span><strong>${value}</strong></div>`).join("")}</div>
      </div>
    </article>`;
  }).join("");
  container.querySelectorAll("[data-edit-id]").forEach(button => button.addEventListener("click", () => openCharacterForm(button.dataset.editId)));
  container.querySelectorAll("[data-delete-id]").forEach(button => button.addEventListener("click", () => deleteCharacter(button.dataset.deleteId)));
}

function renderInitiative() {
  const container = $("#initiative-list");
  container.innerHTML = state.initiative.length ? state.initiative.map((entry, index) => `
    <div class="initiative-entry"><strong>${entry.roll}</strong><span>${escapeHtml(entry.name)}</span><span>${index === 0 ? "Turno atual" : `#${index + 1}`}</span></div>`).join("") :
    '<div class="initiative-empty"><span>☼</span><p>Role iniciativa para<br>começar o combate.</p></div>';
}

function setView(view) {
  document.querySelectorAll(".nav-link").forEach(button => button.classList.toggle("active", button.dataset.view === view));
  $("#table-view").classList.toggle("active-view", view === "table");
  $("#sheets-view").classList.toggle("active-view", view === "sheets");
}

function openCharacterForm(id) {
  const character = state.data.characters.find(item => item.id === id);
  $("#character-form").reset();
  $("#character-id").value = character?.id || "";
  $("#dialog-title").textContent = character ? "Editar personagem" : "Criar personagem";
  $("#character-name").value = character?.name || "";
  $("#character-role").value = character?.role || "";
  $("#character-species").value = character?.species || "";
  $("#character-level").value = character?.level || 1;
  $("#character-hp").value = character?.hitPoints ?? 10;
  $("#character-max-hp").value = character?.maxHitPoints ?? 10;
  $("#character-ac").value = character?.armorClass ?? 10;
  $("#character-notes").value = character?.notes || "";
  $("#character-dialog").showModal();
  $("#character-name").focus();
}

async function saveCharacter(event) {
  event.preventDefault();
  const id = $("#character-id").value;
  const existing = state.data.characters.find(character => character.id === id);
  const payload = {
    id: id || undefined,
    name: $("#character-name").value.trim(),
    role: $("#character-role").value.trim(),
    species: $("#character-species").value.trim(),
    level: Number($("#character-level").value),
    hitPoints: Number($("#character-hp").value),
    maxHitPoints: Number($("#character-max-hp").value),
    armorClass: Number($("#character-ac").value),
    notes: $("#character-notes").value.trim(),
    attributes: existing?.attributes || { Força: 10, Destreza: 10, Constituição: 10, Inteligência: 10, Sabedoria: 10, Carisma: 10 }
  };
  try {
    await api(id ? `/characters/${id}` : "/characters", { method: id ? "PUT" : "POST", body: JSON.stringify(payload) });
    $("#character-dialog").close();
    await loadState();
    notify(id ? "Ficha atualizada." : "Personagem criado e adicionado à mesa.");
  } catch (error) {
    notify(error.message);
  }
}

async function deleteCharacter(id) {
  const character = state.data.characters.find(item => item.id === id);
  if (!character || !window.confirm(`Excluir a ficha de ${character.name}?`)) return;
  try {
    await api(`/characters/${id}`, { method: "DELETE" });
    await loadState();
    notify("Ficha removida da campanha.");
  } catch (error) {
    notify(error.message);
  }
}

function bindTokenDrag(token) {
  token.addEventListener("pointerdown", event => {
    event.preventDefault();
    token.setPointerCapture(event.pointerId);
    token.classList.add("dragging");
  });
  token.addEventListener("pointermove", event => {
    if (!token.classList.contains("dragging")) return;
    const bounds = $("#board").getBoundingClientRect();
    const x = Math.max(0, Math.min(100, (event.clientX - bounds.left) / bounds.width * 100));
    const y = Math.max(0, Math.min(100, (event.clientY - bounds.top) / bounds.height * 100));
    token.style.left = `${x}%`;
    token.style.top = `${y}%`;
  });
  token.addEventListener("pointerup", async event => {
    if (!token.classList.contains("dragging")) return;
    token.classList.remove("dragging");
    const bounds = $("#board").getBoundingClientRect();
    const x = Math.max(0, Math.min(100, (event.clientX - bounds.left) / bounds.width * 100));
    const y = Math.max(0, Math.min(100, (event.clientY - bounds.top) / bounds.height * 100));
    try {
      await api(`/tokens/${token.dataset.tokenId}`, { method: "PATCH", body: JSON.stringify({ x, y }) });
      const saved = state.data.tokens.find(item => item.id === token.dataset.tokenId);
      if (saved) { saved.x = x; saved.y = y; }
    } catch (error) {
      notify(error.message);
      await loadState();
    }
  });
}

async function importMap(file) {
  if (!file) return;
  if (!file.type.startsWith("image/") || file.size > 7 * 1024 * 1024) {
    notify("Escolha uma imagem de até 7 MB.");
    return;
  }
  const imageData = await new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("Não foi possível ler essa imagem."));
    reader.readAsDataURL(file);
  });
  const name = file.name.replace(/\.[^.]+$/, "").trim() || "Novo mapa";
  try {
    await api("/maps", { method: "POST", body: JSON.stringify({ name, imageData }) });
    await loadState();
    notify("Mapa adicionado à campanha.");
  } catch (error) {
    notify(error.message);
  }
}

async function importSheet(file) {
  if (!file) return;
  try {
    const imported = JSON.parse(await file.text());
    const character = imported.character || imported;
    const payload = {
      name: character.name || character.nome || "",
      role: character.role || character.class || character.classe || "",
      species: character.species || character.race || character.raca || "",
      level: Number(character.level || character.nivel || 1),
      hitPoints: Number(character.hitPoints ?? character.hp ?? 10),
      maxHitPoints: Number(character.maxHitPoints ?? character.hpMax ?? character.hitPoints ?? 10),
      armorClass: Number(character.armorClass ?? character.ac ?? 10),
      notes: character.notes || character.notesText || character.notas || "",
      attributes: character.attributes || character.stats || character.atributos || undefined
    };
    if (!payload.name) throw new Error("A ficha precisa ter um campo de nome.");
    await api("/characters", { method: "POST", body: JSON.stringify(payload) });
    await loadState();
    setView("sheets");
    notify("Ficha importada para a campanha.");
  } catch (error) {
    notify(error instanceof SyntaxError ? "Esse arquivo não parece ser uma ficha JSON válida." : error.message);
  }
}

async function addToken() {
  const map = activeMap();
  if (!map) return notify("Importe um mapa antes de adicionar tokens.");
  if (!state.data.characters.length) return openCharacterForm();
  const character = state.data.characters[0];
  try {
    await api("/tokens", { method: "POST", body: JSON.stringify({ characterId: character.id, mapId: map.id, color: "#d9a66c" }) });
    await loadState();
    notify(`Token de ${character.name} adicionado.`);
  } catch (error) {
    notify(error.message);
  }
}

async function rollDice(sides = state.sides, count = 1) {
  const button = $("#roll-button");
  button.disabled = true;
  try {
    const result = await api("/dice/roll", { method: "POST", body: JSON.stringify({ sides, count }) });
    $("#roll-total").textContent = result.total;
    $("#roll-detail").textContent = `${result.rolls.join(" + ")}${result.modifier ? ` ${result.modifier > 0 ? "+" : "−"} ${Math.abs(result.modifier)}` : ""} · d${result.sides}`;
    const resultBox = $("#roll-result");
    resultBox.classList.remove("critical", "failure", "animate");
    if (result.sides === 20 && count === 1 && result.total === 20) resultBox.classList.add("critical");
    if (result.sides === 20 && count === 1 && result.total === 1) resultBox.classList.add("failure");
    void resultBox.offsetWidth;
    resultBox.classList.add("animate");
  } catch (error) {
    notify(error.message);
  } finally {
    button.disabled = false;
  }
}

async function editCampaignName() {
  const name = window.prompt("Nome da campanha", state.data.campaignName);
  if (name === null) return;
  try {
    await api("/campaign", { method: "PUT", body: JSON.stringify({ name }) });
    await loadState();
    notify("Nome da campanha atualizado.");
  } catch (error) {
    notify(error.message);
  }
}

async function rollInitiative() {
  if (!state.data.characters.length) return notify("Crie um personagem antes de rolar iniciativa.");
  try {
    state.initiative = await Promise.all(state.data.characters.map(async character => {
      const result = await api("/dice/roll", { method: "POST", body: JSON.stringify({ sides: 20 }) });
      return {
        name: character.name,
        roll: result.total + Math.floor(((character.attributes?.Destreza ?? 10) - 10) / 2)
      };
    }));
    state.initiative.sort((first, second) => second.roll - first.roll);
    renderInitiative();
    notify("Iniciativa rolada para todos os personagens.");
  } catch (error) {
    notify(error.message);
  }
}

document.querySelectorAll(".nav-link").forEach(button => button.addEventListener("click", () => setView(button.dataset.view)));
$("#campaign-picker").addEventListener("click", editCampaignName);
$("#new-map-shortcut").addEventListener("click", () => $("#map-import").click());
$("#upload-map-button").addEventListener("click", () => $("#map-import").click());
$("#empty-upload-button").addEventListener("click", () => $("#map-import").click());
$("#map-import").addEventListener("change", event => {
  importMap(event.target.files[0]);
  event.target.value = "";
});
$("#new-character-button").addEventListener("click", () => openCharacterForm());
$("#add-character-inline").addEventListener("click", () => openCharacterForm());
$("#new-character-large").addEventListener("click", () => openCharacterForm());
$("#character-form").addEventListener("submit", saveCharacter);
$("#close-dialog").addEventListener("click", () => $("#character-dialog").close());
$("#cancel-dialog").addEventListener("click", () => $("#character-dialog").close());
$("#add-token-button").addEventListener("click", addToken);
$("#grid-button").addEventListener("click", () => $("#map-grid").classList.toggle("hidden-grid"));
$("#zoom-button").addEventListener("click", () => {
  state.zoomed = !state.zoomed;
  $("#map-art").style.transform = state.zoomed ? "scale(1.35)" : "";
  $("#map-grid").style.transform = state.zoomed ? "scale(1.35)" : "";
  $("#map-grid").style.transformOrigin = "center";
  $("#zoom-button").innerHTML = `<span>⊕</span> ${state.zoomed ? "135%" : "100%"}`;
});
$("#dice-row").addEventListener("click", event => {
  const die = event.target.closest("[data-sides]");
  if (!die) return;
  state.sides = Number(die.dataset.sides);
  document.querySelectorAll(".die").forEach(button => button.classList.toggle("active-die", button === die));
  $("#roll-button").innerHTML = `<span>✳</span> Rolar d${state.sides}`;
});
$("#roll-button").addEventListener("click", () => rollDice(state.sides, Number($("#dice-count").value) || 1));
$("#initiative-button").addEventListener("click", rollInitiative);
$("#sheet-import").addEventListener("change", event => {
  importSheet(event.target.files[0]);
  event.target.value = "";
});
$("#invite-button").addEventListener("click", () => notify("Os convites estarão disponíveis em uma próxima etapa."));

loadState();

"use strict";

const GLOBAL_KEYS = [
  { key: "global.iswarmup", type: "bool" },
  { key: "global.isduringround", type: "bool" },
  { key: "global.mapname", type: "string" },
  { key: "global.hashostages", type: "bool" },
];
const ID_PATTERN = /^[a-z0-9_]+$/;
const BOOL_OPERATORS = ["bool==", "bool!="];

let catalog = { operators: [], action_types: [], weapons: [], events: [] };
let state = newChallenge();
let selected = 0;

const $ = (id) => document.getElementById(id);

function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (value === undefined || value === null || value === false) continue;
    if (key === "class") el.className = value;
    else if (key.startsWith("on")) el.addEventListener(key.slice(2), value);
    else if (key === "value") el.value = value;
    else if (key === "checked" || key === "disabled" || key === "selected") el[key] = !!value;
    else el.setAttribute(key, value === true ? "" : value);
  }
  for (const child of children.flat()) {
    if (child === undefined || child === null || child === false) continue;
    el.append(child.nodeType ? child : document.createTextNode(String(child)));
  }
  return el;
}

function newTask(id) {
  return {
    id,
    title: {},
    type: "",
    amount: 1,
    cooldown: 0,
    visible: true,
    announce_progress: true,
    announce_completion: true,
    data: [],
    rules: [],
    actions: [],
    requires: [],
  };
}

function newChallenge() {
  return { id: "new_challenge", title: { en: "" }, tasks: [newTask("easy")] };
}

function uniqueTaskId(base) {
  let candidate = base;
  let n = 2;
  while (state.tasks.some((t) => t.id === candidate)) candidate = `${base}_${n++}`;
  return candidate;
}

function eventForType(type) {
  return catalog.events.find((e) => e.type === type);
}

function normalizeKeyEntry(entry) {
  if (typeof entry === "string") return { key: entry, type: "" };
  return { key: entry.key, type: entry.type || "" };
}

function keyEntriesForType(type) {
  const event = eventForType(type);
  if (!event) return [...GLOBAL_KEYS];
  const fromEvent = event.keys;
  const fromSet = catalog.key_sets?.[event.event_class];
  const raw = fromEvent ?? fromSet ?? [];
  return [...GLOBAL_KEYS, ...raw.map(normalizeKeyEntry)];
}

function keyNamesForType(type) {
  return keyEntriesForType(type).map((e) => e.key);
}

function keyTypeFor(type, key) {
  const hit = keyEntriesForType(type).find((e) => e.key === key);
  return hit?.type || "";
}

function toObject() {
  return {
    title: { ...state.title },
    tasks: state.tasks.map((t) => {
      const out = { id: t.id };
      if (Object.keys(t.title).length) out.title = { ...t.title };
      out.type = t.type;
      out.amount = Number(t.amount) || 0;
      out.cooldown = Number(t.cooldown) || 0;
      if (!t.visible) out.visible = false;
      out.announce_progress = !!t.announce_progress;
      out.announce_completion = !!t.announce_completion;
      const data = {};
      for (const row of t.data) {
        if (!row.plugin || !row.key) continue;
        (data[row.plugin] ??= {})[row.key] = String(row.value);
      }
      if (Object.keys(data).length) out.data = data;
      out.rules = t.rules.map((r) => ({ key: r.key, operator: r.operator, value: String(r.value) }));
      if (t.actions.length) out.actions = t.actions.map((a) => ({ type: a.type, values: a.values.map(String) }));
      if (t.requires.length) out.requires = [...t.requires];
      return out;
    }),
  };
}

function fromObject(obj, fallbackId) {
  if (!obj || typeof obj !== "object" || Array.isArray(obj)) throw new Error("Top level must be a mapping with title and tasks.");
  if (!Array.isArray(obj.tasks)) throw new Error("Missing tasks list. Old dependency-based files must be converted first.");
  const asStrings = (m) => Object.fromEntries(Object.entries(m && typeof m === "object" ? m : {}).map(([k, v]) => [k, String(v ?? "")]));
  const challenge = {
    id: fallbackId || state.id,
    title: asStrings(obj.title),
    tasks: obj.tasks.map((t, i) => {
      const task = newTask(String(t.id ?? `task_${i + 1}`));
      task.title = asStrings(t.title);
      task.type = String(t.type ?? "");
      task.amount = Number(t.amount ?? 1);
      task.cooldown = Number(t.cooldown ?? 0);
      task.visible = t.visible ?? true;
      task.announce_progress = t.announce_progress ?? true;
      task.announce_completion = t.announce_completion ?? true;
      for (const [plugin, values] of Object.entries(t.data ?? {})) {
        for (const [key, value] of Object.entries(values ?? {})) task.data.push({ plugin, key, value: String(value) });
      }
      task.rules = (t.rules ?? []).map((r) => ({ key: String(r.key ?? ""), operator: String(r.operator ?? "=="), value: String(r.value ?? "") }));
      task.actions = (t.actions ?? []).map((a) => ({ type: String(a.type ?? ""), values: (a.values ?? []).map(String) }));
      task.requires = (t.requires ?? []).map(String);
      return task;
    }),
  };
  if (!Object.keys(challenge.title).length) challenge.title = { en: "" };
  if (!challenge.tasks.length) challenge.tasks.push(newTask("easy"));
  return challenge;
}

function toYaml() {
  return jsyaml.dump(toObject(), { lineWidth: -1, noRefs: true, indent: 2 });
}

function validate() {
  const problems = [];
  const add = (level, message, task) => problems.push({ level, message, task });
  const ids = state.tasks.map((t) => t.id);

  if (!ID_PATTERN.test(state.id)) add("error", "Challenge id may only contain a-z, 0-9 and _.");
  if (!Object.values(state.title).some((v) => v.trim())) add("error", "Challenge title needs at least one language with text.");
  if (!state.title.en?.trim()) add("warn", "Challenge title has no English (en) entry.");

  state.tasks.forEach((t, i) => {
    const where = `Task ${i + 1} (${t.id || "?"})`;
    if (!t.id) add("error", `${where}: id is empty.`, i);
    else if (ids.indexOf(t.id) !== i) add("error", `${where}: duplicate id.`, i);
    if (!t.type) add("error", `${where}: no event type selected.`, i);
    else if (!eventForType(t.type)) add("error", `${where}: unknown type "${t.type}".`, i);
    if (!(Number(t.amount) >= 1)) add("error", `${where}: amount must be at least 1.`, i);
    if (Number(t.cooldown) < 0) add("error", `${where}: cooldown must not be negative.`, i);
    if (t.visible && !Object.values(state.title).some((v) => v.trim()) && !Object.values(t.title).some((v) => v.trim())) {
      add("warn", `${where}: visible task without any title.`, i);
    }

    const validKeys = new Set(keyNamesForType(t.type));
    t.rules.forEach((r, ri) => {
      if (!r.key) add("error", `${where}: rule ${ri + 1} has no key.`, i);
      else if (t.type && eventForType(t.type) && !validKeys.has(r.key.toLowerCase())) add("warn", `${where}: rule key "${r.key}" is not provided by ${t.type}.`, i);
      if (!catalog.operators.includes(r.operator)) add("error", `${where}: rule ${ri + 1} uses unknown operator "${r.operator}".`, i);
      if (BOOL_OPERATORS.includes(r.operator) && !["true", "false"].includes(r.value.toLowerCase())) add("error", `${where}: rule ${ri + 1} needs true or false.`, i);
    });

    t.actions.forEach((a, ai) => {
      if (!catalog.action_types.includes(a.type)) {
        add("error", `${where}: action ${ai + 1} has unknown type "${a.type}".`, i);
        return;
      }
      if (a.type === "server.runcommand") {
        if (!a.values[0]?.trim()) add("error", `${where}: action ${ai + 1} needs a command.`, i);
        return;
      }
      if (!a.values.length) add("warn", `${where}: action ${ai + 1} targets no tasks.`, i);
      for (const v of a.values) if (!ids.includes(v)) add("error", `${where}: action ${ai + 1} references unknown task "${v}".`, i);
    });

    for (const req of t.requires) {
      if (req === t.id) add("error", `${where}: requires itself.`, i);
      else if (!ids.includes(req)) add("error", `${where}: requires unknown task "${req}".`, i);
    }
  });

  const byId = new Map(state.tasks.map((t) => [t.id, t]));
  const visiting = new Set();
  const done = new Set();
  const reported = new Set();
  const visit = (id, path) => {
    if (done.has(id) || !byId.has(id)) return;
    if (visiting.has(id)) {
      const cycle = [...path.slice(path.indexOf(id)), id];
      const key = [...cycle].sort().join(",");
      if (!reported.has(key)) {
        reported.add(key);
        add("error", `Cyclic requires: ${cycle.join(" -> ")}.`, state.tasks.findIndex((t) => t.id === id));
      }
      return;
    }
    visiting.add(id);
    for (const req of byId.get(id).requires) if (req !== id) visit(req, [...path, id]);
    visiting.delete(id);
    done.add(id);
  };
  state.tasks.forEach((t) => visit(t.id, []));
  return problems;
}

function renameTask(task, newId) {
  const old = task.id;
  task.id = newId;
  if (!old) return;
  for (const t of state.tasks) {
    t.requires = t.requires.map((r) => (r === old ? newId : r));
    for (const a of t.actions) if (a.type !== "server.runcommand") a.values = a.values.map((v) => (v === old ? newId : v));
  }
}

function removeTaskReferences(id) {
  for (const t of state.tasks) {
    t.requires = t.requires.filter((r) => r !== id);
    for (const a of t.actions) if (a.type !== "server.runcommand") a.values = a.values.filter((v) => v !== id);
  }
}

function textInput(value, onInput, attrs = {}) {
  return h("input", { type: "text", spellcheck: "false", value, ...attrs, oninput: (e) => onInput(e.target.value) });
}

function langRows(map, onChange, { minimum = 0 } = {}) {
  const wrap = h("div");
  const entries = Object.entries(map);
  if (!entries.length) wrap.append(h("div", { class: "empty" }, "No languages."));
  entries.forEach(([lang, text], index) => {
    wrap.append(
      h(
        "div",
        { class: "row" },
        h("input", {
          class: "lang",
          type: "text",
          value: lang,
          title: "Language code",
          spellcheck: "false",
          onchange: (e) => {
            const code = e.target.value.trim().toLowerCase();
            if (!code || (code !== lang && code in map)) {
              e.target.value = lang;
              return;
            }
            const rebuilt = {};
            for (const [k, v] of Object.entries(map)) rebuilt[k === lang ? code : k] = v;
            for (const k of Object.keys(map)) delete map[k];
            Object.assign(map, rebuilt);
            onChange(true);
          },
        }),
        h("input", { class: "grow", type: "text", value: text, placeholder: "{count}/{total} Title", oninput: (e) => { map[lang] = e.target.value; onChange(false); } }),
        h("button", {
          class: "iconbtn",
          title: "Remove language",
          disabled: entries.length <= minimum,
          onclick: () => { delete map[lang]; onChange(true); },
        }, "×"),
      ),
    );
  });
  return wrap;
}

function addLang(map) {
  const known = ["en", "de", "fr", "es", "ru", "pl", "pt", "tr"];
  const next = known.find((k) => !(k in map)) ?? `l${Object.keys(map).length + 1}`;
  map[next] = "";
}

function renderChallengePanel() {
  $("challenge-id").value = state.id;
  $("file-hint").textContent = `${state.id || "challenge"}.yaml`;
  const wrap = $("challenge-title");
  wrap.replaceChildren(langRows(state.title, (structural) => (structural ? renderAll() : refresh()), { minimum: 1 }));
}

function renderTaskList() {
  const problems = validate();
  const withErrors = new Set(problems.filter((p) => p.level === "error" && p.task !== undefined).map((p) => p.task));
  const list = $("task-list");
  list.replaceChildren(
    ...state.tasks.map((t, i) =>
      h(
        "li",
        { class: `task-item${i === selected ? " active" : ""}${withErrors.has(i) ? " error" : ""}`, onclick: () => { selected = i; renderAll(); } },
        h("div", { class: "label" }, h("b", {}, t.id || "(no id)"), h("span", {}, t.type || "no type")),
        t.visible ? null : h("span", { class: "tag" }, "hidden"),
        h(
          "div",
          { class: "tools", onclick: (e) => e.stopPropagation() },
          h("button", { class: "iconbtn", title: "Move up", disabled: i === 0, onclick: () => moveTask(i, -1) }, "↑"),
          h("button", { class: "iconbtn", title: "Move down", disabled: i === state.tasks.length - 1, onclick: () => moveTask(i, 1) }, "↓"),
          h("button", { class: "iconbtn", title: "Duplicate", onclick: () => duplicateTask(i) }, "⧉"),
          h("button", { class: "iconbtn", title: "Delete", onclick: () => deleteTask(i) }, "🗑"),
        ),
      ),
    ),
  );
}

function moveTask(index, delta) {
  const target = index + delta;
  if (target < 0 || target >= state.tasks.length) return;
  [state.tasks[index], state.tasks[target]] = [state.tasks[target], state.tasks[index]];
  selected = target;
  renderAll();
}

function duplicateTask(index) {
  const copy = structuredClone(state.tasks[index]);
  copy.id = uniqueTaskId(`${copy.id || "task"}_copy`);
  state.tasks.splice(index + 1, 0, copy);
  selected = index + 1;
  renderAll();
}

function deleteTask(index) {
  if (state.tasks.length === 1) {
    state.tasks[0] = newTask("easy");
  } else {
    const [removed] = state.tasks.splice(index, 1);
    removeTaskReferences(removed.id);
  }
  selected = Math.min(selected, state.tasks.length - 1);
  renderAll();
}

function section(title, onAdd, ...body) {
  return h("div", {}, h("h3", {}, title, onAdd ? h("button", { class: "btn small", onclick: onAdd }, "Add") : null), ...body);
}

function renderEditor() {
  const task = state.tasks[selected];
  const root = $("editor");
  if (!task) {
    root.replaceChildren();
    return;
  }

  const keyList = $("dl-keys");
  keyList.replaceChildren(
    ...keyEntriesForType(task.type).map((e) =>
      h("option", { value: e.key }, e.type ? `${e.key} (${e.type})` : e.key),
    ),
  );

  const otherIds = state.tasks.filter((t) => t !== task && t.id).map((t) => t.id);
  const typeOptions = [
    h("option", { value: "", selected: !task.type }, "Select an event type"),
    ...catalog.events.map((e) => h("option", { value: e.type, selected: e.type === task.type }, `${e.type}  (${e.event_class})`)),
  ];
  if (task.type && !eventForType(task.type)) typeOptions.push(h("option", { value: task.type, selected: true }, `${task.type} (unknown)`));

  const card = h(
    "section",
    { class: "card" },
    h("h2", {}, `Task ${selected + 1}: ${task.id || "(no id)"}`),
    h(
      "div",
      { class: "grid-2" },
      h("label", { class: "field" }, h("span", {}, "Task id"), h("input", { type: "text", spellcheck: "false", value: task.id, onchange: (e) => { renameTask(task, e.target.value.trim()); renderAll(); } })),
      h("label", { class: "field" }, h("span", {}, "Event type"), h("select", { onchange: (e) => { task.type = e.target.value; renderAll(); } }, ...typeOptions)),
    ),
    h(
      "div",
      { class: "grid-2" },
      h("label", { class: "field" }, h("span", {}, "Amount"), h("input", { type: "number", min: "1", value: task.amount, oninput: (e) => { task.amount = e.target.value; refresh(); } })),
      h("label", { class: "field" }, h("span", {}, "Cooldown (seconds)"), h("input", { type: "number", min: "0", value: task.cooldown, oninput: (e) => { task.cooldown = e.target.value; refresh(); } })),
    ),
    h(
      "div",
      { class: "checks" },
      checkbox("Visible", task.visible, (v) => { task.visible = v; refresh(); }),
      checkbox("Announce progress", task.announce_progress, (v) => { task.announce_progress = v; refresh(); }),
      checkbox("Announce completion", task.announce_completion, (v) => { task.announce_completion = v; refresh(); }),
    ),

    section(
      "Task title (optional, falls back to challenge title)",
      () => { addLang(task.title); renderAll(); },
      langRows(task.title, (structural) => (structural ? renderAll() : refresh())),
    ),

    section("Requires", null, requiresEditor(task, otherIds)),

    section(
      "Rules",
      () => { task.rules.push({ key: "", operator: "==", value: "" }); renderAll(); },
      task.rules.length ? task.rules.map((r, i) => ruleRow(task, r, i)) : h("div", { class: "empty" }, "No rules: every matching event counts."),
    ),

    section(
      "Actions",
      () => { task.actions.push({ type: catalog.action_types[0] ?? "", values: [] }); renderAll(); },
      task.actions.length ? task.actions.map((a, i) => actionBlock(task, a, i)) : h("div", { class: "empty" }, "No actions."),
    ),

    section(
      "Data (passed to other plugins)",
      () => { task.data.push({ plugin: "", key: "", value: "" }); renderAll(); },
      task.data.length ? task.data.map((d, i) => dataRow(task, d, i)) : h("div", { class: "empty" }, "No data."),
    ),
  );
  root.replaceChildren(card);
}

function checkbox(label, checked, onChange) {
  return h("label", { class: "check" }, h("input", { type: "checkbox", checked, onchange: (e) => onChange(e.target.checked) }), label);
}

function requiresEditor(task, otherIds) {
  if (!otherIds.length) return h("div", { class: "empty" }, "No other tasks.");
  const unknown = task.requires.filter((r) => !otherIds.includes(r));
  return h(
    "div",
    { class: "checks" },
    ...otherIds.map((id) =>
      checkbox(id, task.requires.includes(id), (on) => {
        task.requires = on ? [...task.requires, id] : task.requires.filter((r) => r !== id);
        refresh();
      }),
    ),
    ...unknown.map((id) => h("span", { class: "tag" }, `unknown: ${id}`)),
  );
}

function ruleRow(task, rule, index) {
  const valueList = /weapon|item/.test(rule.key) ? "dl-weapons" : undefined;
  const operators = catalog.operators.includes(rule.operator) ? catalog.operators : [rule.operator, ...catalog.operators];
  const keyType = keyTypeFor(task.type, rule.key);
  return h(
    "div",
    { class: "row" },
    h("input", {
      class: "keyw",
      type: "text",
      spellcheck: "false",
      list: "dl-keys",
      value: rule.key,
      placeholder: "key",
      title: keyType ? `${rule.key} (${keyType})` : rule.key,
      oninput: (e) => { rule.key = e.target.value; refresh(); },
      onchange: renderAll,
    }),
    h("span", { class: "keytype", title: "Value type" }, keyType || ""),
    h("select", { class: "opw", onchange: (e) => { rule.operator = e.target.value; refresh(); } }, ...operators.map((o) => h("option", { value: o, selected: o === rule.operator }, o))),
    h("input", { class: "grow", type: "text", spellcheck: "false", list: valueList, value: rule.value, placeholder: "value", oninput: (e) => { rule.value = e.target.value; refresh(); } }),
    h("button", { class: "iconbtn", title: "Remove rule", onclick: () => { task.rules.splice(index, 1); renderAll(); } }, "×"),
  );
}

function actionBlock(task, action, index) {
  const types = catalog.action_types.includes(action.type) ? catalog.action_types : [action.type, ...catalog.action_types];
  const head = h(
    "div",
    { class: "row" },
    h(
      "select",
      { class: "grow", onchange: (e) => { action.type = e.target.value; action.values = []; renderAll(); } },
      ...types.map((t) => h("option", { value: t, selected: t === action.type }, t)),
    ),
    h("button", { class: "iconbtn", title: "Remove action", onclick: () => { task.actions.splice(index, 1); renderAll(); } }, "×"),
  );

  let body;
  if (action.type === "server.runcommand") {
    body = textInput(action.values[0] ?? "", (v) => { action.values = [v]; refresh(); }, { placeholder: "say {steamid} did it" });
  } else {
    body = h(
      "div",
      { class: "checks" },
      ...state.tasks.filter((t) => t.id).map((t) =>
        checkbox(t.id, action.values.includes(t.id), (on) => {
          action.values = on ? [...action.values, t.id] : action.values.filter((v) => v !== t.id);
          refresh();
        }),
      ),
    );
  }
  return h("div", { class: "subcard" }, head, body);
}

function dataRow(task, row, index) {
  return h(
    "div",
    { class: "row" },
    h("input", { class: "keyw", type: "text", spellcheck: "false", value: row.plugin, placeholder: "plugin", oninput: (e) => { row.plugin = e.target.value; refresh(); } }),
    h("input", { class: "keyw", type: "text", spellcheck: "false", value: row.key, placeholder: "key", oninput: (e) => { row.key = e.target.value; refresh(); } }),
    h("input", { class: "grow", type: "text", spellcheck: "false", value: row.value, placeholder: "value", oninput: (e) => { row.value = e.target.value; refresh(); } }),
    h("button", { class: "iconbtn", title: "Remove data", onclick: () => { task.data.splice(index, 1); renderAll(); } }, "×"),
  );
}

function renderOutput() {
  const problems = validate();
  const list = $("problems");
  if (!problems.length) {
    list.replaceChildren(h("li", { class: "ok" }, "No problems found."));
  } else {
    list.replaceChildren(
      ...problems.map((p) =>
        h("li", { class: p.level, style: p.task !== undefined ? "cursor:pointer" : undefined, onclick: p.task !== undefined ? () => { selected = p.task; renderAll(); } : undefined }, p.message),
      ),
    );
  }
  $("yaml-preview").textContent = toYaml();
}

function refresh() {
  $("file-hint").textContent = `${state.id || "challenge"}.yaml`;
  renderTaskList();
  renderOutput();
}

function renderAll() {
  renderChallengePanel();
  renderTaskList();
  renderEditor();
  renderOutput();
}

function importText(text, fileName) {
  try {
    const parsed = jsyaml.load(text);
    const id = fileName ? fileName.replace(/\.(ya?ml)$/i, "").toLowerCase().replace(/[^a-z0-9_]+/g, "_") : state.id;
    state = fromObject(parsed, id);
    selected = 0;
    renderAll();
  } catch (error) {
    alert(`Could not import YAML:\n${error.message}`);
  }
}

function download(name, text) {
  const url = URL.createObjectURL(new Blob([text], { type: "text/yaml" }));
  const link = h("a", { href: url, download: name });
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function bindToolbar() {
  $("challenge-id").addEventListener("input", (e) => {
    state.id = e.target.value.trim();
    refresh();
  });
  $("add-challenge-lang").addEventListener("click", () => {
    addLang(state.title);
    renderAll();
  });
  $("add-task").addEventListener("click", () => {
    state.tasks.push(newTask(uniqueTaskId(["easy", "medium", "hard"][state.tasks.length] ?? "task")));
    selected = state.tasks.length - 1;
    renderAll();
  });
  $("btn-new").addEventListener("click", () => {
    if (!confirm("Discard the current challenge?")) return;
    state = newChallenge();
    selected = 0;
    renderAll();
  });
  $("file-import").addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (file) importText(await file.text(), file.name);
    e.target.value = "";
  });
  $("btn-paste").addEventListener("click", () => {
    $("paste-text").value = "";
    $("paste-dialog").showModal();
  });
  $("paste-confirm").addEventListener("click", () => {
    const text = $("paste-text").value;
    if (text.trim()) importText(text);
  });
  $("btn-copy").addEventListener("click", async () => {
    await navigator.clipboard.writeText(toYaml());
    const btn = $("btn-copy");
    btn.textContent = "Copied";
    setTimeout(() => (btn.textContent = "Copy YAML"), 1200);
  });
  $("btn-export").addEventListener("click", () => {
    const errors = validate().filter((p) => p.level === "error");
    if (errors.length && !confirm(`${errors.length} validation error(s) remain. Export anyway?`)) return;
    download(`${state.id || "challenge"}.yaml`, toYaml());
  });
}

async function init() {
  bindToolbar();
  try {
    const response = await fetch("catalog.json");
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    catalog = await response.json();
  } catch (error) {
    alert(`Could not load catalog.json: ${error.message}`);
  }
  $("dl-weapons").replaceChildren(...catalog.weapons.map((w) => h("option", { value: w })));
  renderAll();
}

init();

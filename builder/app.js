"use strict";

const WIKI = "https://github.com/Kandru/cs2-challenges/wiki/";
const GLOBAL_KEYS = [
  { key: "global.iswarmup", type: "bool" },
  { key: "global.isduringround", type: "bool" },
  { key: "global.mapname", type: "string" },
  { key: "global.hashostages", type: "bool" },
];
const ID_PATTERN = /^[a-z0-9_]+$/;
const BOOL_OPS = ["bool==", "bool!="];
const NUMBER_OPS = ["==", "!=", "<", ">", "<=", ">="];
const STRING_OPS = ["==", "!=", "contains", "!contains"];
const LIVE_FILTERS = [
  { key: "global.iswarmup", operator: "bool==", value: "false" },
  { key: "global.isduringround", operator: "bool==", value: "true" },
  { key: "victim.isbot", operator: "bool==", value: "false" },
  { key: "player.isbot", operator: "bool==", value: "false" },
  { key: "isteamkill", operator: "bool==", value: "false" },
  { key: "isselfkill", operator: "bool==", value: "false" },
];

let catalog = { operators: [], action_types: [], weapons: [], events: [], key_sets: {} };
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

function helpLink(path, title = "Open documentation") {
  return h("a", { class: "helplink", href: WIKI + path, target: "_blank", rel: "noopener", title }, "?");
}

function labelHelp(text, path) {
  return h("span", { class: "label-row" }, text, " ", helpLink(path));
}

function empty(text) {
  return h("div", { class: "empty" }, text);
}

function iconBtn(title, onclick, disabled = false) {
  return h("button", { class: "iconbtn", type: "button", title, disabled, onclick }, "×");
}

function options(values, selected, label = (v) => v) {
  return values.map((v) => h("option", { value: v, selected: v === selected }, label(v)));
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
  return { id: "new_challenge", title: { en: "", de: "" }, data: [], tasks: [newTask("easy")] };
}

function uniqueTaskId(base) {
  let id = base;
  let n = 2;
  while (state.tasks.some((t) => t.id === id)) id = `${base}_${n++}`;
  return id;
}

function eventForType(type) {
  return catalog.events.find((e) => e.type === type);
}

function keyLookup(type) {
  const event = eventForType(type);
  const raw = event ? (event.keys ?? catalog.key_sets?.[event.event_class] ?? []) : [];
  const entries = [
    ...GLOBAL_KEYS,
    ...raw.map((e) => (typeof e === "string" ? { key: e, type: "" } : { key: e.key, type: e.type || "" })),
  ];
  const byKey = new Map(entries.map((e) => [e.key.toLowerCase(), e]));
  return {
    entries,
    has: (key) => byKey.has(String(key).toLowerCase()),
    typeOf: (key) => byKey.get(String(key).toLowerCase())?.type || "",
  };
}

function isBool(type) {
  return /^bool/i.test(type || "");
}

function operatorsFor(type) {
  if (isBool(type)) return BOOL_OPS;
  if (/^(int|uint|long|ulong|short|byte|float|double|number)/i.test(type || "")) return NUMBER_OPS;
  return STRING_OPS;
}

function coerceRule(rule, keyType) {
  const ops = operatorsFor(keyType);
  if (!ops.includes(rule.operator)) rule.operator = ops[0];
  if (isBool(keyType) && !["true", "false"].includes(String(rule.value).toLowerCase())) rule.value = "true";
}

function dataToObject(rows) {
  const data = {};
  for (const { plugin, key, value } of rows) {
    if (!plugin || !key) continue;
    (data[plugin] ??= {})[key] = String(value);
  }
  return data;
}

function dataToRows(obj) {
  return Object.entries(obj ?? {}).flatMap(([plugin, values]) =>
    Object.entries(values ?? {}).map(([key, value]) => ({ plugin, key, value: String(value) })),
  );
}

function toObject() {
  const out = { title: { ...state.title } };
  const challengeData = dataToObject(state.data);
  if (Object.keys(challengeData).length) out.data = challengeData;
  out.tasks = state.tasks.map((t) => {
    const task = {
      id: t.id,
      type: t.type,
      amount: Number(t.amount) || 0,
      cooldown: Number(t.cooldown) || 0,
      announce_progress: !!t.announce_progress,
      announce_completion: !!t.announce_completion,
      rules: t.rules.map((r) => ({ key: r.key, operator: r.operator, value: String(r.value) })),
    };
    if (Object.keys(t.title).length) task.title = { ...t.title };
    if (!t.visible) task.visible = false;
    const data = dataToObject(t.data);
    if (Object.keys(data).length) task.data = data;
    if (t.actions.length) task.actions = t.actions.map((a) => ({ type: a.type, values: a.values.map(String) }));
    if (t.requires.length) task.requires = [...t.requires];
    return task;
  });
  return out;
}

function fromObject(obj, fallbackId) {
  if (!obj || typeof obj !== "object" || Array.isArray(obj)) throw new Error("Top level must be a mapping with title and tasks.");
  if (!Array.isArray(obj.tasks)) throw new Error("Missing tasks list. Old dependency-based files must be converted first.");
  const asStrings = (m) =>
    Object.fromEntries(Object.entries(m && typeof m === "object" ? m : {}).map(([k, v]) => [k, String(v ?? "")]));
  const challenge = {
    id: fallbackId || state.id,
    title: asStrings(obj.title),
    data: dataToRows(obj.data),
    tasks: obj.tasks.map((t, i) => {
      const task = newTask(String(t.id ?? `task_${i + 1}`));
      task.title = asStrings(t.title);
      task.type = String(t.type ?? "");
      task.amount = Number(t.amount ?? 1);
      task.cooldown = Number(t.cooldown ?? 0);
      task.visible = t.visible ?? true;
      task.announce_progress = t.announce_progress ?? true;
      task.announce_completion = t.announce_completion ?? true;
      task.data = dataToRows(t.data);
      task.rules = (t.rules ?? []).map((r) => ({
        key: String(r.key ?? ""),
        operator: String(r.operator ?? "=="),
        value: String(r.value ?? ""),
      }));
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

function validateData(rows, prefix, taskIndex) {
  const problems = [];
  rows.forEach((row, i) => {
    if ((row.plugin || row.key || row.value) && (!row.plugin || !row.key)) {
      problems.push({ level: "error", message: `${prefix} data row ${i + 1}: plugin and key are required.`, task: taskIndex });
    }
  });
  return problems;
}

function validate() {
  const problems = [];
  const add = (level, message, task) => problems.push({ level, message, task });
  const ids = state.tasks.map((t) => t.id);

  if (!ID_PATTERN.test(state.id)) add("error", "Challenge id may only contain a-z, 0-9 and _.");
  if (!Object.values(state.title).some((v) => v.trim())) add("error", "Challenge title needs at least one language with text.");
  problems.push(...validateData(state.data, "Challenge"));

  state.tasks.forEach((t, i) => {
    const where = `Task ${i + 1} (${t.id || "?"})`;
    if (!t.id) add("error", `${where}: id is empty.`, i);
    else if (!ID_PATTERN.test(t.id)) add("error", `${where}: id may only contain a-z, 0-9 and _.`, i);
    else if (ids.indexOf(t.id) !== i) add("error", `${where}: duplicate id.`, i);
    if (!t.type) add("error", `${where}: no event type selected.`, i);
    else if (!eventForType(t.type)) add("error", `${where}: unknown type "${t.type}".`, i);
    if (!(Number(t.amount) >= 1)) add("error", `${where}: amount must be at least 1.`, i);
    if (Number(t.cooldown) < 0) add("error", `${where}: cooldown must not be negative.`, i);
    if (t.visible && !Object.values(t.title).some((v) => v.trim()) && !Object.values(state.title).some((v) => v.trim())) {
      add("warn", `${where}: visible task without any title.`, i);
    }
    problems.push(...validateData(t.data, where, i));

    const keys = keyLookup(t.type);
    t.rules.forEach((r, ri) => {
      if (!r.key) add("error", `${where}: rule ${ri + 1} has no key.`, i);
      else if (t.type && eventForType(t.type) && !keys.has(r.key)) {
        add("error", `${where}: rule key "${r.key}" is not provided by ${t.type} (unknown keys never match).`, i);
      }
      const keyType = keys.typeOf(r.key);
      const ops = operatorsFor(keyType);
      if (!ops.includes(r.operator) && !catalog.operators.includes(r.operator)) {
        add("error", `${where}: rule ${ri + 1} uses unknown operator "${r.operator}".`, i);
      } else if (keyType && !ops.includes(r.operator)) {
        add("error", `${where}: rule ${ri + 1}: operator "${r.operator}" does not fit key type ${keyType}.`, i);
      }
      if (!String(r.value).trim()) add("error", `${where}: rule ${ri + 1} has an empty value.`, i);
      if (BOOL_OPS.includes(r.operator) && !["true", "false"].includes(String(r.value).toLowerCase())) {
        add("error", `${where}: rule ${ri + 1} needs true or false.`, i);
      }
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
      if (!a.values.length) add("error", `${where}: action ${ai + 1} targets no tasks.`, i);
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

function errors() {
  return validate().filter((p) => p.level === "error");
}

function rewriteTaskId(oldId, newId) {
  if (!oldId || oldId === newId) return;
  for (const t of state.tasks) {
    t.requires = t.requires.map((r) => (r === oldId ? newId : r));
    for (const a of t.actions) {
      if (a.type !== "server.runcommand") a.values = a.values.map((v) => (v === oldId ? newId : v));
    }
  }
}

function removeTaskReferences(id) {
  for (const t of state.tasks) {
    t.requires = t.requires.filter((r) => r !== id);
    for (const a of t.actions) {
      if (a.type !== "server.runcommand") a.values = a.values.filter((v) => v !== id);
    }
  }
}

function langRows(map, onChange, { minimum = 0 } = {}) {
  const entries = Object.entries(map);
  if (!entries.length) return empty("No languages.");
  return h(
    "div",
    {},
    ...entries.map(([lang, text]) =>
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
            const rebuilt = Object.fromEntries(Object.entries(map).map(([k, v]) => [k === lang ? code : k, v]));
            Object.keys(map).forEach((k) => delete map[k]);
            Object.assign(map, rebuilt);
            onChange(true);
          },
        }),
        h("input", {
          class: "grow",
          type: "text",
          value: text,
          placeholder: "{count}/{total} Title",
          oninput: (e) => {
            map[lang] = e.target.value;
            onChange(false);
          },
        }),
        iconBtn("Remove language", () => {
          delete map[lang];
          onChange(true);
        }, entries.length <= minimum),
      ),
    ),
  );
}

function addLang(map) {
  const known = ["en", "de", "fr", "es", "ru", "pl", "pt", "tr"];
  map[known.find((k) => !(k in map)) ?? `l${Object.keys(map).length + 1}`] = "";
}

function dataEditor(rows, onStructural) {
  if (!rows.length) return empty("No data rows.");
  return h(
    "div",
    {},
    ...rows.map((row, index) =>
      h(
        "div",
        { class: "row" },
        ...["plugin", "key", "value"].map((field) =>
          h("input", {
            class: field === "value" ? "grow" : "keyw",
            type: "text",
            spellcheck: "false",
            value: row[field],
            placeholder: field,
            oninput: (e) => {
              row[field] = e.target.value;
              refresh();
            },
          }),
        ),
        iconBtn("Remove data", () => {
          rows.splice(index, 1);
          onStructural();
        }),
      ),
    ),
  );
}

function checkbox(label, checked, onChange, helpPath) {
  return h(
    "label",
    { class: "check" },
    h("input", { type: "checkbox", checked, onchange: (e) => onChange(e.target.checked) }),
    label,
    helpPath ? [" ", helpLink(helpPath)] : null,
  );
}

function section(title, helpPath, onAdd, ...body) {
  return h(
    "div",
    {},
    h(
      "h3",
      {},
      h("span", { class: "label-row" }, title, helpPath ? [" ", helpLink(helpPath)] : null),
      onAdd ? h("button", { class: "btn small", type: "button", onclick: onAdd }, "Add") : null,
    ),
    ...body,
  );
}

function field(label, help, ...body) {
  return h("label", { class: "field" }, labelHelp(label, help), ...body);
}

function numberField(label, help, value, min, onInput, hint) {
  return field(
    label,
    help,
    h("input", {
      type: "number",
      min: String(min),
      value,
      oninput: (e) => onInput(e.target.value),
    }),
    hint ? h("small", {}, hint) : null,
  );
}

function addLiveFilters(task) {
  if (!task.type) return;
  const keys = keyLookup(task.type);
  let added = false;
  for (const rule of LIVE_FILTERS) {
    if (!keys.has(rule.key)) continue;
    if (task.rules.some(
      (r) =>
        r.key.toLowerCase() === rule.key.toLowerCase() &&
        r.operator === rule.operator &&
        String(r.value).toLowerCase() === rule.value.toLowerCase(),
    )) continue;
    task.rules.push({ ...rule });
    added = true;
  }
  if (added) renderAll();
}

function addRuleBreaker(task) {
  if (!task.type || !task.id) {
    alert("Select an event type and set a task id first.");
    return;
  }
  const breakerId = uniqueTaskId(`${task.id}_breaker`);
  const mark = task.actions.find((a) => a.type === "task.mark_completed");
  if (mark) {
    if (!mark.values.includes(breakerId)) mark.values.push(breakerId);
  } else {
    task.actions.push({ type: "task.mark_completed", values: [breakerId] });
  }

  const breaker = newTask(breakerId);
  Object.assign(breaker, {
    type: task.type,
    amount: 1,
    visible: false,
    announce_progress: false,
    announce_completion: false,
    actions: [
      { type: "notify.player.progress.rule_broken", values: [task.id] },
      { type: "task.reset_progress", values: [task.id] },
      { type: "task.reset_completed", values: [breakerId] },
    ],
  });

  const index = state.tasks.indexOf(task);
  state.tasks.splice(index + 1, 0, breaker);
  selected = index + 1;
  renderAll();
}

function renderChallengePanel() {
  $("challenge-id").value = state.id;
  $("file-hint").textContent = `${state.id || "challenge"}.yaml`;
  $("challenge-title").replaceChildren(langRows(state.title, (structural) => (structural ? renderAll() : refresh()), { minimum: 1 }));
  $("challenge-data").replaceChildren(dataEditor(state.data, renderAll));
}

function renderTaskList() {
  const withErrors = new Set(validate().filter((p) => p.level === "error" && p.task !== undefined).map((p) => p.task));
  $("task-list").replaceChildren(
    ...state.tasks.map((t, i) =>
      h(
        "li",
        {
          class: `task-item${i === selected ? " active" : ""}${withErrors.has(i) ? " error" : ""}`,
          onclick: () => {
            selected = i;
            renderAll();
          },
        },
        h("div", { class: "label" }, h("b", {}, t.id || "(no id)"), h("span", {}, t.type || "no type")),
        t.visible ? null : h("span", { class: "tag" }, "hidden"),
        h(
          "div",
          { class: "tools", onclick: (e) => e.stopPropagation() },
          h("button", { class: "iconbtn", type: "button", title: "Move up", disabled: i === 0, onclick: () => moveTask(i, -1) }, "↑"),
          h("button", {
            class: "iconbtn",
            type: "button",
            title: "Move down",
            disabled: i === state.tasks.length - 1,
            onclick: () => moveTask(i, 1),
          }, "↓"),
          h("button", { class: "iconbtn", type: "button", title: "Duplicate", onclick: () => duplicateTask(i) }, "⧉"),
          h("button", { class: "iconbtn", type: "button", title: "Delete", onclick: () => deleteTask(i) }, "🗑"),
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
    removeTaskReferences(state.tasks.splice(index, 1)[0].id);
  }
  selected = Math.min(selected, state.tasks.length - 1);
  renderAll();
}

function requiresEditor(task, otherIds) {
  if (!otherIds.length) return empty("No other tasks yet.");
  return h(
    "div",
    { class: "checks" },
    ...otherIds.map((id) =>
      checkbox(id, task.requires.includes(id), (on) => {
        task.requires = on ? [...task.requires, id] : task.requires.filter((r) => r !== id);
        refresh();
      }),
    ),
    ...task.requires.filter((r) => !otherIds.includes(r)).map((id) => h("span", { class: "tag" }, `unknown: ${id}`)),
  );
}

function ruleRow(task, rule, index, keys) {
  const keyType = keys.typeOf(rule.key);
  coerceRule(rule, keyType);
  const weaponLike = /weapon|item/.test(rule.key);
  const keyOptions = [
    h("option", { value: "", selected: !rule.key }, "Select a key"),
    ...keys.entries.map((e) =>
      h("option", { value: e.key, selected: e.key.toLowerCase() === String(rule.key).toLowerCase() }, e.type ? `${e.key} (${e.type})` : e.key),
    ),
  ];
  if (rule.key && !keys.has(rule.key)) keyOptions.push(h("option", { value: rule.key, selected: true }, `${rule.key} (unknown)`));

  let valueControl;
  if (isBool(keyType)) {
    rule.value = String(rule.value).toLowerCase() === "false" ? "false" : "true";
    valueControl = h(
      "select",
      {
        class: "grow",
        onchange: (e) => {
          rule.value = e.target.value;
          refresh();
        },
      },
      ...options(["true", "false"], rule.value),
    );
  } else {
    valueControl = h("input", {
      class: "grow",
      type: "text",
      spellcheck: "false",
      list: weaponLike ? "dl-weapons" : undefined,
      value: rule.value,
      placeholder: "value",
      oninput: (e) => {
        rule.value = e.target.value;
        refresh();
      },
    });
  }

  return h(
    "div",
    { class: "rule-block" },
    h(
      "div",
      { class: "row" },
      h(
        "select",
        {
          class: "keyw",
          onchange: (e) => {
            rule.key = e.target.value;
            coerceRule(rule, keys.typeOf(rule.key));
            renderAll();
          },
        },
        ...keyOptions,
      ),
      h("span", { class: "keytype", title: "Value type" }, keyType || "?"),
      h(
        "select",
        {
          class: "opw",
          onchange: (e) => {
            rule.operator = e.target.value;
            refresh();
          },
        },
        ...options(operatorsFor(keyType), rule.operator),
      ),
      valueControl,
      iconBtn("Remove rule", () => {
        task.rules.splice(index, 1);
        renderAll();
      }),
    ),
    weaponLike
      ? h("small", { class: "rule-hint" }, "Prefer ", h("code", {}, "contains"), " with the short weapon name (e.g. ", h("code", {}, "ak47"), ").")
      : null,
  );
}

function actionBlock(task, action, index) {
  const types = catalog.action_types.includes(action.type) ? catalog.action_types : [action.type, ...catalog.action_types];
  return h(
    "div",
    { class: "subcard" },
    h(
      "div",
      { class: "row" },
      h(
        "select",
        {
          class: "grow",
          onchange: (e) => {
            action.type = e.target.value;
            action.values = [];
            renderAll();
          },
        },
        ...options(types, action.type),
      ),
      helpLink(`actions#${action.type.replace(/\./g, "-")}`, "Open action documentation"),
      iconBtn("Remove action", () => {
        task.actions.splice(index, 1);
        renderAll();
      }),
    ),
    action.type === "server.runcommand"
      ? h(
          "div",
          {},
          h("input", {
            type: "text",
            spellcheck: "false",
            value: action.values[0] ?? "",
            placeholder: "say {steamid} did it",
            oninput: (e) => {
              action.values = [e.target.value];
              refresh();
            },
          }),
          h("small", {}, "Placeholders: {steamid}, {userid}, {index}."),
        )
      : h(
          "div",
          { class: "checks" },
          ...state.tasks.filter((t) => t.id).map((t) =>
            checkbox(t.id, action.values.includes(t.id), (on) => {
              action.values = on ? [...action.values, t.id] : action.values.filter((v) => v !== t.id);
              refresh();
            }),
          ),
        ),
  );
}

function renderEditor() {
  const task = state.tasks[selected];
  const root = $("editor");
  if (!task) {
    root.replaceChildren();
    return;
  }

  const otherIds = state.tasks.filter((t) => t !== task && t.id).map((t) => t.id);
  const event = eventForType(task.type);
  const keys = keyLookup(task.type);
  const typeOptions = [
    h("option", { value: "", selected: !task.type }, "Select an event type"),
    ...catalog.events.map((e) => h("option", { value: e.type, selected: e.type === task.type }, `${e.type}  (${e.event_class})`)),
  ];
  if (task.type && !event) typeOptions.push(h("option", { value: task.type, selected: true }, `${task.type} (unknown)`));

  root.replaceChildren(
    h(
      "section",
      { class: "card" },
      h("h2", {}, `Task ${selected + 1}: ${task.id || "(no id)"}`),
      h(
        "div",
        { class: "grid-2" },
        field(
          "Task id",
          "blueprints#task-id",
          h("input", {
            type: "text",
            spellcheck: "false",
            value: task.id,
            onchange: (e) => {
              const next = e.target.value.trim();
              rewriteTaskId(task.id, next);
              task.id = next;
              renderAll();
            },
          }),
        ),
        h(
          "label",
          { class: "field" },
          h(
            "span",
            { class: "label-row" },
            "Event type ",
            helpLink("events"),
            " ",
            helpLink(event ? `events/${event.event_class}` : "events", event ? `Open ${event.event_class} keys` : "Open events"),
          ),
          h(
            "select",
            {
              onchange: (e) => {
                task.type = e.target.value;
                const lookup = keyLookup(task.type);
                for (const rule of task.rules) coerceRule(rule, lookup.typeOf(rule.key));
                renderAll();
              },
            },
            ...typeOptions,
          ),
          h("small", {}, event ? ["Rule keys for ", h("code", {}, event.event_class), "."] : "Pick the game event this task listens for."),
        ),
      ),
      h(
        "div",
        { class: "grid-2" },
        numberField("Amount", "blueprints#amount", task.amount, 1, (v) => {
          task.amount = v;
          refresh();
        }, "How many matching events are needed."),
        numberField("Cooldown (seconds)", "blueprints#cooldown", task.cooldown, 0, (v) => {
          task.cooldown = v;
          refresh();
        }, "Wait this many seconds before counting again."),
      ),
      h(
        "div",
        { class: "checks" },
        checkbox("Visible on HUD", task.visible, (v) => {
          task.visible = v;
          refresh();
        }, "blueprints#visible"),
        checkbox("Announce progress", task.announce_progress, (v) => {
          task.announce_progress = v;
          refresh();
        }, "blueprints#announce"),
        checkbox("Announce completion", task.announce_completion, (v) => {
          task.announce_completion = v;
          refresh();
        }, "blueprints#announce"),
      ),
      h("p", { class: "hint" }, "Hidden tasks stay off the HUD and do not count toward solved. A challenge is done when every visible task is done."),

      section(
        "Task title (optional)",
        "blueprints#task-title",
        () => {
          addLang(task.title);
          renderAll();
        },
        langRows(task.title, (structural) => (structural ? renderAll() : refresh())),
        h("small", {}, "Supports {count} and {total}. Empty title uses the challenge title."),
      ),
      section(
        "Unlock after",
        "blueprints#requires",
        null,
        requiresEditor(task, otherIds),
        h("small", {}, "Empty = available immediately (can run in parallel). Several checked = all must be done (AND)."),
      ),
      section(
        "Rules (all must pass)",
        "rules",
        () => {
          const first = keys.entries[0] || { key: "", type: "string" };
          task.rules.push({
            key: first.key,
            operator: operatorsFor(first.type)[0],
            value: isBool(first.type) ? "true" : "",
          });
          renderAll();
        },
        h(
          "div",
          { class: "toolbar-row" },
          h("button", {
            class: "btn small",
            type: "button",
            disabled: !task.type,
            title: "Add warmup/round and common kill filters when available",
            onclick: () => addLiveFilters(task),
          }, "Add live filters"),
          helpLink("rules#global-keys", "Global keys documentation"),
          h("small", {}, "Only adds filters this event supports."),
        ),
        task.rules.length ? task.rules.map((r, i) => ruleRow(task, r, i, keys)) : empty("No rules: every matching event counts."),
        h("p", { class: "hint" }, "Every rule must pass (AND). There is no OR. Unknown keys never match. ", helpLink("rules#operators"), " operators."),
      ),
      section(
        "Actions (when this task finishes)",
        "actions",
        () => {
          task.actions.push({ type: catalog.action_types[0] ?? "", values: [] });
          renderAll();
        },
        h(
          "div",
          { class: "toolbar-row" },
          h("button", {
            class: "btn small",
            type: "button",
            disabled: !task.type || !task.id,
            onclick: () => addRuleBreaker(task),
          }, "Add rule breaker"),
          helpLink("actions#streak-broken", "Streak / rule-breaker pattern"),
          h("small", {}, "Creates a hidden task below this one. Set the “bad” rules on that task."),
        ),
        task.actions.length ? task.actions.map((a, i) => actionBlock(task, a, i)) : empty("No actions."),
        h("p", { class: "hint" }, "Values are task ids in this file, except server.runcommand."),
      ),
      section(
        "Task data (other plugins)",
        "plugin-integration#challenge-data",
        () => {
          task.data.push({ plugin: "", key: "", value: "" });
          renderAll();
        },
        dataEditor(task.data, renderAll),
        h("small", {}, "Optional payload on this task’s progress/completion. This plugin does not grant rewards."),
      ),
    ),
  );
}

function renderOutput() {
  const problems = validate();
  $("problems").replaceChildren(
    ...(problems.length
      ? problems.map((p) =>
          h(
            "li",
            {
              class: p.level,
              style: p.task !== undefined ? "cursor:pointer" : undefined,
              onclick: p.task !== undefined
                ? () => {
                    selected = p.task;
                    renderAll();
                  }
                : undefined,
            },
            p.message,
          ),
        )
      : [h("li", { class: "ok" }, "No problems found.")]),
  );
  $("yaml-preview").textContent = toYaml();
  const err = problems.filter((p) => p.level === "error");
  $("btn-export").disabled = err.length > 0;
  $("btn-export").title = err.length ? `${err.length} validation error(s) — fix them before export` : "Download YAML";
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
  $("add-challenge-data").addEventListener("click", () => {
    state.data.push({ plugin: "", key: "", value: "" });
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
    const err = errors();
    if (err.length) {
      alert(`Fix ${err.length} validation error(s) before copying.`);
      return;
    }
    await navigator.clipboard.writeText(toYaml());
    const btn = $("btn-copy");
    btn.textContent = "Copied";
    setTimeout(() => (btn.textContent = "Copy YAML"), 1200);
  });
  $("btn-export").addEventListener("click", () => {
    const err = errors();
    if (err.length) {
      alert(`Fix ${err.length} validation error(s) before export.`);
      return;
    }
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
  $("dl-weapons").replaceChildren(...(catalog.weapons || []).map((w) => h("option", { value: w })));
  renderAll();
}

init();

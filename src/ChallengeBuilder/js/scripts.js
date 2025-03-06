document.addEventListener('DOMContentLoaded', () => {
    const typeSelect = document.getElementById('type');
    const rulesContainer = document.getElementById('rules-container');
    const challengesList = document.getElementById('challenges-list');
    const uploadChallenges = document.getElementById('upload-challenges');
    let challenges = {};
    let allRules = {};
    let operators = [];

    // load challenge metadata
    fetch('data/challenge-metadata.json')
        .then(response => response.json())
        .then(data => {
            const { version, types, operators: ops } = data;
            document.querySelector('.navbar-brand').textContent += ` v${version}`;
            operators = ops;
            allRules = types;
            for (const entry in allRules) {
                const option = document.createElement('option');
                option.value = entry;
                option.textContent = allRules[entry].name;
                typeSelect.appendChild(option);
            }
            typeSelect.addEventListener('change', () => {
                rulesContainer.innerHTML = '';
            });
            // Trigger change event to load rules for the default selected type
            typeSelect.dispatchEvent(new Event('change'));
        });

    // when user adds a rule
    document.getElementById('add-rule').addEventListener('click', () => {
        addRuleElement('', '', '');
    });

    // when user saves a challenge
    document.getElementById('save-challenge').addEventListener('click', () => {
        const type = typeSelect.value;
        const title = document.getElementById('title').value;
        let slug = document.getElementById('slug').value;
        slug = slug.replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_]/g, '');
        const points = document.getElementById('points').value;
        const amount = document.getElementById('amount').value;
        const rules = Array.from(document.querySelectorAll('#rules-container .rule')).map(ruleDiv => {
            const keyElement = ruleDiv.querySelector('[name="key"]');
            const operatorElement = ruleDiv.querySelector('[name="operator"]');
            const valueElement = ruleDiv.querySelector('[name="value"]');
            if (keyElement && operatorElement && valueElement) {
                const key = keyElement.value;
                const operator = operatorElement.value;
                let value;
                if (valueElement.type == "checkbox") {
                    value = valueElement.checked;
                } else {
                    value = valueElement.value;
                }
                if (key && operator && value !== null) {
                    return { key, operator, value };
                }
            }
            return null;
        }).filter(rule => rule !== null);
        // build challenge object
        const challenge = { title, type, points, amount, rules };
        // check if challenge already exists
        if (!(slug in challenges)) {
            // add challenge to the list
            appendChallengeToList(slug, challenge);
        }
        // save challenge
        challenges[slug] = challenge;
    });

    // download finished challenges file
    document.getElementById('download-challenges').addEventListener('click', () => {
        const blob = new Blob([JSON.stringify({ blueprints: challenges }, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'challenges.json';
        a.click();
        URL.revokeObjectURL(url);
    });

    // upload challenges file
    uploadChallenges.addEventListener('change', (event) => {
        const file = event.target.files[0];
        const reader = new FileReader();
        reader.onload = (e) => {
            const uploadedChallenges = JSON.parse(e.target.result).blueprints;
            challenges = { ...challenges, ...uploadedChallenges };
            challengesList.innerHTML = '';
            for (const slug in challenges) {
                appendChallengeToList(slug, challenges[slug]);
            }
        };
        reader.readAsText(file);
    });

    // add rule element to the DOM
    function addRuleElement(key = '', operator = '', value = '') {
        const ruleDiv = document.createElement('div');
        ruleDiv.classList.add('rule', 'd-flex', 'align-items-center', 'mb-2');
        const keySelect = document.createElement('select');
        keySelect.name = 'key';
        keySelect.classList.add('form-select', 'bg-dark', 'text-light', 'me-2');
        const selectedType = typeSelect.value;
        const rules = allRules[selectedType].rules;
        rules.forEach(rule => {
            const option = document.createElement('option');
            option.value = rule.slug;
            option.textContent = rule.name;
            if (rule.slug === key) {
                option.selected = true;
            }
            keySelect.appendChild(option);
        });
        ruleDiv.appendChild(keySelect);

        const operatorSelect = document.createElement('select');
        operatorSelect.name = 'operator';
        operatorSelect.classList.add('form-select', 'bg-dark', 'text-light', 'me-2');
        operators.forEach(op => {
            const option = document.createElement('option');
            option.value = op;
            option.textContent = op;
            if (op === operator) {
                option.selected = true;
            }
            operatorSelect.appendChild(option);
        });
        ruleDiv.appendChild(operatorSelect);

        const valueInput = document.createElement('input');
        valueInput.name = 'value';
        valueInput.value = value;
        valueInput.classList.add('form-control', 'bg-dark', 'text-light', 'me-2');
        valueInput.type = allRules[typeSelect.value].rules.find(rule => rule.slug === keySelect.value).type;
        if (valueInput.type == "checkbox") {
            if (!valueInput.classList.contains('form-check-input')) valueInput.classList.add('form-check-input');
            valueInput.checked = (value === 'true' || value === true);
            if (operatorSelect.selectedIndex == 0) {
                operatorSelect.selectedIndex = Array.from(operatorSelect.options).findIndex(option => option.value === 'bool==');
            }
        }else{
            valueInput.classList.remove('form-check-input');
        }
        ruleDiv.appendChild(valueInput);

        const deleteButton = document.createElement('button');
        deleteButton.type = 'button';
        deleteButton.textContent = 'Delete';
        deleteButton.classList.add('btn', 'btn-danger');
        deleteButton.addEventListener('click', () => {
            rulesContainer.removeChild(ruleDiv);
        });
        ruleDiv.appendChild(deleteButton);
        const ruleDescDiv = document.createElement('div');
        ruleDescDiv.classList.add('rule', 'd-flex', 'align-items-center', 'mb-2');
        const ruleDescText = document.createElement('div');
        ruleDescText.textContent = allRules[typeSelect.value].rules.find(rule => rule.slug === keySelect.value).description;
        ruleDescDiv.appendChild(ruleDescText);
        keySelect.addEventListener('change', () => {
            valueInput.type = allRules[typeSelect.value].rules.find(rule => rule.slug === keySelect.value).type;
            ruleDescText.textContent = allRules[typeSelect.value].rules.find(rule => rule.slug === keySelect.value).description;
            if (valueInput.type == "checkbox") {
                if (!valueInput.classList.contains('form-check-input')) valueInput.classList.add('form-check-input');
                valueInput.checked = false;
                operatorSelect.selectedIndex = Array.from(operatorSelect.options).findIndex(option => option.value === 'bool==');
            } else {
                if (valueInput.classList.contains('form-check-input')) valueInput.classList.remove('form-check-input');
                valueInput.value = "";
                operatorSelect.selectedIndex = 0;
            }
        });
        rulesContainer.appendChild(ruleDiv);
        rulesContainer.appendChild(ruleDescDiv);
    }

    function appendChallengeToList(slug, challenge)
    {
        // add challenge to the list
        const li = document.createElement('li');
        const titleDiv = document.createElement('div');
        titleDiv.textContent = challenge.title;
        const slugDiv = document.createElement('div');
        slugDiv.textContent = slug;
        slugDiv.classList.add('text-light', 'small');
        li.classList.add('list-group-item', 'list-group-item-action', 'bg-dark', 'text-light');
        // Add edit button
        const editButton = document.createElement('button');
        editButton.type = 'button';
        editButton.textContent = 'Edit';
        editButton.classList.add('btn', 'btn-secondary', 'ms-3');
        editButton.addEventListener('click', () => {
            typeSelect.value = challenges[slug].type;
            document.getElementById('title').value = challenges[slug].title;
            document.getElementById('slug').value = slug;
            document.getElementById('points').value = challenges[slug].points;
            document.getElementById('amount').value = challenges[slug].amount;
            rulesContainer.innerHTML = '';
            challenges[slug].rules.forEach(rule => {
                addRuleElement(rule.key, rule.operator, rule.value);
            });
            window.scrollTo(0, 0);
        });
        // Add delete button
        const deleteButton = document.createElement('button');
        deleteButton.type = 'button';
        deleteButton.textContent = 'Delete';
        deleteButton.classList.add('btn', 'btn-danger', 'ms-3');
        deleteButton.addEventListener('click', () => {
            challengesList.removeChild(li);
            delete challenges[slug];
        });
        const textContainer = document.createElement('div');
        textContainer.classList.add('flex-grow-1');
        textContainer.appendChild(titleDiv);
        textContainer.appendChild(slugDiv);
        const buttonContainer = document.createElement('div');
        buttonContainer.classList.add('ms-auto');
        buttonContainer.appendChild(editButton);
        buttonContainer.appendChild(deleteButton);
        li.classList.add('d-flex', 'align-items-center');
        li.appendChild(textContainer);
        li.appendChild(buttonContainer);
        // Add click event to load challenge data
        challengesList.appendChild(li);
    }
});
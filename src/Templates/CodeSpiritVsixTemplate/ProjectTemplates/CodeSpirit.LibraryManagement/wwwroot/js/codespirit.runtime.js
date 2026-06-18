(function () {
  'use strict';

  function selector(name, value) {
    return '[' + name + '="' + String(value).replace(/"/g, '\\"') + '"]';
  }

  function getViewModelRoot(element) {
    return element.closest('[data-cs-vm]');
  }

  function serializeForm(form) {
    var data = {};
    var formData = new FormData(form);
    formData.forEach(function (value, key) {
      data[key] = value;
    });
    return data;
  }

  function postViewModel(form, payload) {
    return fetch(form.getAttribute('action') || window.location.pathname, {
      method: form.getAttribute('method') || 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).then(function (response) {
      if (!response.ok) {
        throw new Error('ViewModel request failed: ' + response.status);
      }
      return response.json();
    });
  }

  function readCommand(submitter) {
    if (!submitter) {
      return null;
    }

    return submitter.getAttribute('data-cs-command') || (submitter.name === '__command' ? submitter.value : null);
  }

  function updateField(root, name, value) {
    if (!root || !name) {
      return;
    }

    root.querySelectorAll(selector('name', name) + ', ' + selector('data-cs-bind', name)).forEach(function (element) {
      if ('value' in element) {
        element.value = value == null ? '' : value;
      } else {
        element.textContent = value == null ? '' : value;
      }
    });
  }

  function applyState(root, state) {
    Object.keys(state || {}).forEach(function (key) {
      updateField(root, key, state[key]);
    });
  }

  function mount(root) {
    var target = root || document;
    if (window.CodeSpirit && window.CodeSpirit.ui && typeof window.CodeSpirit.ui.init === 'function') {
      window.CodeSpirit.ui.init(target);
    }
    return target;
  }

  function refresh(root) {
    return mount(root);
  }

  function emit(root, name, detail) {
    root.dispatchEvent(new CustomEvent(name, {
      bubbles: true,
      detail: detail
    }));
  }

  function handleSubmit(event) {
    var form = event.target;
    if (!form.matches('[data-cs-vm]')) {
      return;
    }

    event.preventDefault();
    var payload = serializeForm(form);
    var submitter = event.submitter;
    var command = readCommand(submitter);
    if (command) {
      payload.__command = command;
    } else if (submitter && submitter.name) {
      payload[submitter.name] = submitter.value;
    }

    postViewModel(form, payload).then(function (result) {
      applyState(form, result.state || result);
      emit(form, 'codespirit:updated', result);
    }).catch(function (error) {
      emit(form, 'codespirit:error', error);
    });
  }

  function handleInput(event) {
    var detail = event.detail || {};
    var target = event.target;
    var root = getViewModelRoot(target);
    var name = detail.name || target.name || target.getAttribute('data-cs-bind');
    var value = detail.value != null ? detail.value : target.value;

    updateField(root, name, value);
    if (root && name) {
      emit(root, 'codespirit:changed', { name: name, value: value });
    }
  }

  function input(element, name, value) {
    if (!element) {
      return;
    }

    element.dispatchEvent(new CustomEvent('codespirit:input', {
      bubbles: true,
      detail: {
        name: name || element.name || element.getAttribute('data-cs-bind'),
        value: arguments.length > 2 ? value : element.value
      }
    }));
  }

  document.addEventListener('submit', handleSubmit);
  document.addEventListener('input', handleInput);
  document.addEventListener('codespirit:input', handleInput);

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.applyState = applyState;
  window.CodeSpirit.input = input;
  window.CodeSpirit.mount = mount;
  window.CodeSpirit.refresh = refresh;
  window.CodeSpirit.updateField = updateField;
})();

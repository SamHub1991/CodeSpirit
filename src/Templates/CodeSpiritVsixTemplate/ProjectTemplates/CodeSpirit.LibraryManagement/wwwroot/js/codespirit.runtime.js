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
    var targetUrl = form.getAttribute('action') || (window.location && window.location.pathname) || '/';
    return fetch(targetUrl, {
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

  function applyRegions(root, regions) {
    Object.keys(regions || {}).forEach(function (name) {
      var current = document.querySelector(selector('data-cs-region', name));
      if (!current) {
        return;
      }

      var host = document.createElement('div');
      host.innerHTML = regions[name];
      var next = host.firstElementChild;
      if (!next) {
        return;
      }

      current.replaceWith(next);
      mount(next);
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
      applyRegions(form, result.regions);
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

  function VmChain(root) {
    this._root = root;
    this._form = root.matches('[data-cs-vm]') ? root : (root.querySelector('[data-cs-vm]') || root.closest('[data-cs-vm]'));
    if (!this._form) {
      throw new Error('CodeSpirit.vm: no ViewModel form found in the element tree');
    }
  }

  VmChain.prototype = {
    set: function (name, value) {
      if (arguments.length === 1 && typeof name === 'object') {
        var self = this;
        Object.keys(name).forEach(function (k) { self.set(k, name[k]); });
        return this;
      }
      updateField(this._form, name, value);
      var el = this._form.querySelector('[name="' + name + '"]');
      if (el) {
        el.value = value == null ? '' : String(value);
        input(el, name, value);
      }
      return this;
    },

    get: function (name) {
      var el = this._form.querySelector('[name="' + name + '"]');
      return el ? el.value : undefined;
    },

    val: function (name, value) {
      return arguments.length < 2 ? this.get(name) : this.set(name, value);
    },

    state: function () {
      return serializeForm(this._form);
    },

    invoke: function (command) {
      var form = this._form;
      var payload = serializeForm(form);
      if (command) {
        payload.__command = command;
      }

      return postViewModel(form, payload).then(function (result) {
        applyRegions(form, result.regions);
        applyState(form, result.state || result);
        emit(form, 'codespirit:updated', result);
        return result;
      });
    },

    on: function (event, handler) {
      this._root.addEventListener(event, handler);
      return this;
    },

    off: function (event, handler) {
      this._root.removeEventListener(event, handler);
      return this;
    },

    once: function (event, handler) {
      var self = this;
      var wrapped = function (e) {
        self.off(event, wrapped);
        handler.call(this, e);
      };
      return this.on(event, wrapped);
    },

    refresh: function () {
      refresh(this._root);
      return this;
    },

    el: function (query) {
      return this._form.querySelector(query);
    },

    all: function (query) {
      return Array.from(this._form.querySelectorAll(query));
    }
  };

  function vm(root) {
    if (typeof root === 'string') {
      root = document.querySelector(root);
    }
    if (!root) {
      throw new Error('CodeSpirit.vm: element not found');
    }
    return new VmChain(root);
  }

  document.addEventListener('submit', handleSubmit);
  document.addEventListener('input', handleInput);
  document.addEventListener('codespirit:input', handleInput);

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.applyState = applyState;
  window.CodeSpirit.applyRegions = applyRegions;
  window.CodeSpirit.input = input;
  window.CodeSpirit.mount = mount;
  window.CodeSpirit.refresh = refresh;
  window.CodeSpirit.updateField = updateField;
  window.CodeSpirit.vm = vm;
  window.CodeSpirit.VmChain = VmChain;
  if (!window.$cs) {
    window.$cs = window.CodeSpirit;
  }
})();

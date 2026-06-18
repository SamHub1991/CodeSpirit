(function () {
  'use strict';

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

  function applyState(root, state) {
    Object.keys(state || {}).forEach(function (key) {
      root.querySelectorAll('[data-cs-bind="' + key + '"]').forEach(function (element) {
        if ('value' in element) {
          element.value = state[key] == null ? '' : state[key];
        } else {
          element.textContent = state[key] == null ? '' : state[key];
        }
      });
    });
  }

  function handleSubmit(event) {
    var form = event.target;
    if (!form.matches('[data-cs-vm]')) {
      return;
    }

    event.preventDefault();
    var payload = serializeForm(form);
    var submitter = event.submitter;
    if (submitter && submitter.name) {
      payload[submitter.name] = submitter.value;
    }

    postViewModel(form, payload).then(function (result) {
      applyState(form, result.state || result);
      form.dispatchEvent(new CustomEvent('codespirit:updated', {
        bubbles: true,
        detail: result
      }));
    }).catch(function (error) {
      form.dispatchEvent(new CustomEvent('codespirit:error', {
        bubbles: true,
        detail: error
      }));
    });
  }

  document.addEventListener('submit', handleSubmit);

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.applyState = applyState;
})();

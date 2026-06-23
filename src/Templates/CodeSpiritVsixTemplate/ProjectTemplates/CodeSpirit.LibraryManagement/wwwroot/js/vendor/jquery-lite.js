(function (global) {
  'use strict';

  var doc = global.document;
  var slice = Array.prototype.slice;
  var push = Array.prototype.push;
  var concat = Array.prototype.concat;

  function isArrayLike(value) {
    return value && typeof value !== 'string' && typeof value.length === 'number';
  }

  function unique(elements) {
    var seen = [];
    var result = [];
    elements.forEach(function (el) {
      if (seen.indexOf(el) === -1) {
        seen.push(el);
        result.push(el);
      }
    });
    return result;
  }

  function toNodeArray(input, context) {
    if (!input) {
      return [];
    }

    if (input instanceof JQLite) {
      return input.toArray();
    }

    if (typeof input === 'function') {
      ready(input);
      return [];
    }

    if (input === global || input === doc || input.nodeType === 1 || input.nodeType === 9) {
      return [input]
    }

    if (typeof input === 'string') {
      if (input.charAt(0) === '<' && input.charAt(input.length - 1) === '>') {
        var container = doc.createElement('div');
        container.innerHTML = input;
        return slice.call(container.children);
      }

      var root = context;
      if (root instanceof JQLite) {
        root = root.length ? root.get(0) : doc;
      }
      if (!root || !root.querySelectorAll) {
        root = doc;
      }

      return slice.call(root.querySelectorAll(input));
    }

    if (isArrayLike(input)) {
      return slice.call(input);
    }

    return [];
  }

  function matches(element, selector) {
    var fn = element.matches || element.msMatchesSelector || element.webkitMatchesSelector;
    return fn ? fn.call(element, selector) : false;
  }

  function ready(callback) {
    if (doc.readyState === 'complete' || doc.readyState === 'interactive') {
      callback();
      return;
    }

    doc.addEventListener('DOMContentLoaded', callback, { once: true });
  }

  function JQLite(elements, prevObject) {
    this.prevObject = prevObject || null;
    this.length = 0;
    push.apply(this, unique(elements || []));
  }

  JQLite.prototype = {
    constructor: JQLite,

    toArray: function () {
      return slice.call(this);
    },

    get: function (index) {
      if (index == null) {
        return this.toArray();
      }

      var normalized = index < 0 ? this.length + index : index;
      return this[normalized];
    },

    eq: function (index) {
      var element = this.get(index);
      return new JQLite(element ? [element] : [], this);
    },

    first: function () {
      return this.eq(0);
    },

    each: function (callback) {
      this.toArray().forEach(function (element, index) {
        callback.call(element, index, element);
      });
      return this;
    },

    find: function (selector) {
      var results = [];
      this.each(function () {
        if (this.querySelectorAll) {
          results = results.concat(slice.call(this.querySelectorAll(selector)));
        }
      });
      return new JQLite(results, this);
    },

    addBack: function (selector) {
      var current = this.toArray();
      var previous = this.prevObject ? this.prevObject.toArray() : [];
      var merged = concat.call(current, previous);

      if (selector) {
        merged = merged.filter(function (element) {
          return matches(element, selector);
        });
      }

      return new JQLite(unique(merged), this.prevObject);
    },

    not: function (selector) {
      return new JQLite(this.toArray().filter(function (element) {
        return !matches(element, selector);
      }), this.prevObject);
    },

    closest: function (selector) {
      var results = [];
      this.each(function () {
        var current = this;
        while (current) {
          if (matches(current, selector)) {
            results.push(current);
            break;
          }
          current = current.parentElement;
        }
      });
      return new JQLite(unique(results), this);
    },

    on: function (events, selectorOrHandler, handler) {
      var selector = null;
      var callback = selectorOrHandler;

      if (typeof selectorOrHandler === 'string') {
        selector = selectorOrHandler;
        callback = handler;
      }

      if (typeof callback !== 'function') {
        return this;
      }

      return this.each(function () {
        var element = this;
        events.split(/\s+/).forEach(function (eventName) {
          if (!eventName) {
            return;
          }

          element.addEventListener(eventName, function (event) {
            if (selector) {
              var target = event.target && event.target.closest ? event.target.closest(selector) : null;
              if (!target || !element.contains(target)) {
                return;
              }
              callback.call(target, event);
              return;
            }

            callback.call(element, event);
          });
        });
      });
    },

    attr: function (name, value) {
      if (typeof name === 'object') {
        var attrs = name;
        return this.each(function () {
          var element = this;
          Object.keys(attrs).forEach(function (key) {
            element.setAttribute(key, attrs[key]);
          });
        });
      }

      if (value === undefined) {
        var first = this.get(0);
        return first && first.getAttribute ? first.getAttribute(name) : undefined;
      }

      return this.each(function () {
        if (value === null) {
          this.removeAttribute(name);
          return;
        }

        this.setAttribute(name, value);
      });
    },

    css: function (name, value) {
      if (typeof name === 'object') {
        var styles = name;
        return this.each(function () {
          var element = this;
          Object.keys(styles).forEach(function (key) {
            element.style[key] = styles[key];
          });
        });
      }

      if (value === undefined) {
        var first = this.get(0);
        return first && first.style ? first.style[name] : undefined;
      }

      return this.each(function () {
        this.style[name] = value;
      });
    },

    addClass: function (name) {
      var classes = String(name || '').split(/\s+/).filter(Boolean);
      return this.each(function () {
        if (this.classList) {
          this.classList.add.apply(this.classList, classes);
        }
      });
    },

    removeClass: function (name) {
      var classes = String(name || '').split(/\s+/).filter(Boolean);
      return this.each(function () {
        if (this.classList) {
          this.classList.remove.apply(this.classList, classes);
        }
      });
    },

    toggleClass: function (name, force) {
      return this.each(function () {
        if (this.classList) {
          this.classList.toggle(name, force);
        }
      });
    },

    html: function (value) {
      if (value === undefined) {
        var first = this.get(0);
        return first ? first.innerHTML : undefined;
      }

      return this.each(function () {
        this.innerHTML = value;
      });
    },

    text: function (value) {
      if (value === undefined) {
        var first = this.get(0);
        return first ? first.textContent : undefined;
      }

      return this.each(function () {
        this.textContent = value;
      });
    },

    val: function (value) {
      if (value === undefined) {
        var first = this.get(0);
        return first && 'value' in first ? first.value : undefined;
      }

      return this.each(function () {
        if ('value' in this) {
          this.value = value;
        }
      });
    },

    append: function (content) {
      return this.each(function () {
        var element = this;
        var nodes = toNodeArray(content, element);
        nodes.forEach(function (node) {
          element.appendChild(node.cloneNode ? node.cloneNode(true) : node);
        });
      });
    },

    appendTo: function (target) {
      $(target).append(this);
      return this;
    }
  };

  function $(input, context) {
    return new JQLite(toNodeArray(input, context));
  }

  $.fn = JQLite.prototype;
  $.fn.constructor = JQLite;

  $.extend = function () {
    var target = arguments[0] || {};
    for (var i = 1; i < arguments.length; i += 1) {
      var source = arguments[i] || {};
      Object.keys(source).forEach(function (key) {
        target[key] = source[key];
      });
    }
    return target;
  };

  $.trim = function (value) {
    return String(value == null ? '' : value).trim();
  };

  $.param = function (value) {
    if (!value) {
      return '';
    }

    return Object.keys(value).map(function (key) {
      return encodeURIComponent(key) + '=' + encodeURIComponent(value[key]);
    }).join('&');
  };

  $.ready = ready;

  $.ajax = function (options) {
    var settings = $.extend({ method: 'GET', headers: {} }, options || {});
    var method = String(settings.method || 'GET').toUpperCase();
    var payload = settings.data;
    var fetchOptions = {
      method: method,
      headers: settings.headers
    };

    if (payload != null && method === 'GET') {
      var query = $.param(payload);
      if (query) {
        settings.url += (settings.url.indexOf('?') >= 0 ? '&' : '?') + query;
      }
    } else if (payload != null) {
      if (settings.contentType === 'application/x-www-form-urlencoded') {
        fetchOptions.body = $.param(payload);
        fetchOptions.headers['Content-Type'] = 'application/x-www-form-urlencoded; charset=utf-8';
      } else {
        fetchOptions.body = typeof payload === 'string' ? payload : JSON.stringify(payload);
        fetchOptions.headers['Content-Type'] = settings.contentType || 'application/json';
      }
    }

    return global.fetch(settings.url, fetchOptions).then(function (response) {
      if (!response.ok) {
        var error = new Error('AJAX request failed: ' + response.status);
        error.status = response.status;
        throw error;
      }

      if (settings.dataType === 'text') {
        return response.text();
      }

      if (settings.dataType === 'blob') {
        return response.blob();
      }

      return response.text().then(function (body) {
        try {
          return body ? JSON.parse(body) : null;
        } catch (_) {
          return body;
        }
      });
    }).then(function (result) {
      if (typeof settings.success === 'function') {
        settings.success(result);
      }
      if (typeof settings.complete === 'function') {
        settings.complete(result);
      }
      return result;
    }).catch(function (error) {
      if (typeof settings.error === 'function') {
        settings.error(error);
      }
      if (typeof settings.complete === 'function') {
        settings.complete(null, error);
      }
      throw error;
    });
  };

  $.get = function (url, data) {
    return $.ajax({ url: url, data: data, method: 'GET' });
  };

  $.post = function (url, data) {
    return $.ajax({ url: url, data: data, method: 'POST' });
  };

  $.getJSON = function (url, data) {
    return $.ajax({ url: url, data: data, method: 'GET', dataType: 'json' });
  };

  $.fn.datepicker = function () {
    return this.each(function () {
      if (this.tagName === 'INPUT') {
        this.setAttribute('type', 'date');
      }
    });
  };

  global.$ = global.jQuery = $;
})(window);

(function () {
  'use strict';

  var CodeSpirit = window.CodeSpirit;
  if (!CodeSpirit) return;

  var TOKEN_ID = 1, TOKEN_NUM = 2, TOKEN_STR = 3, TOKEN_OP = 4, TOKEN_LPAREN = 5, TOKEN_RPAREN = 6, TOKEN_QMARK = 7, TOKEN_COLON = 8, TOKEN_COMMA = 9, TOKEN_EOF = 10;

  function Lexer(input) {
    this.input = input;
    this.pos = 0;
  }

  Lexer.prototype.peek = function () {
    var saved = this.pos;
    var tok = this.next();
    this.pos = saved;
    return tok;
  };

  Lexer.prototype.next = function () {
    var s = this.input;
    var p = this.pos;
    while (p < s.length && (s[p] === ' ' || s[p] === '\t' || s[p] === '\n')) p++;

    if (p >= s.length) { this.pos = p; return { type: TOKEN_EOF }; }

    var c = s[p];

    if (c === '(') { this.pos = p + 1; return { type: TOKEN_LPAREN }; }
    if (c === ')') { this.pos = p + 1; return { type: TOKEN_RPAREN }; }
    if (c === '?') { this.pos = p + 1; return { type: TOKEN_QMARK }; }
    if (c === ':') { this.pos = p + 1; return { type: TOKEN_COLON }; }
    if (c === ',') { this.pos = p + 1; return { type: TOKEN_COMMA }; }

    if (c === "'") {
      var end = p + 1;
      var val = '';
      while (end < s.length) {
        if (s[end] === '\\' && end + 1 < s.length) {
          val += s[end + 1];
          end += 2;
          continue;
        }
        if (s[end] === "'") break;
        val += s[end];
        end++;
      }
      if (end >= s.length || s[end] !== "'") throw new Error('unterminated string literal');
      this.pos = end + 1;
      return { type: TOKEN_STR, value: val };
    }

    if ((c >= '0' && c <= '9') || (c === '.')) {
      var end2 = p;
      while (end2 < s.length && ((s[end2] >= '0' && s[end2] <= '9') || s[end2] === '.')) end2++;
      var numVal = parseFloat(s.substring(p, end2));
      this.pos = end2;
      return { type: TOKEN_NUM, value: numVal };
    }

    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c === '_') {
      var end3 = p;
      while (end3 < s.length && ((s[end3] >= 'a' && s[end3] <= 'z') || (s[end3] >= 'A' && s[end3] <= 'Z') || (s[end3] >= '0' && s[end3] <= '9') || s[end3] === '_')) end3++;
      var word = s.substring(p, end3);
      this.pos = end3;

      if (word === 'contains' || word === 'empty' || word === 'true' || word === 'false' || word === 'null' ||
        word === 'length' || word === 'trim' || word === 'lower' || word === 'upper' ||
        word === 'substring' || word === 'startsWith' || word === 'endsWith' || word === 'indexOf' ||
        word === 'abs' || word === 'min' || word === 'max' || word === 'round' || word === 'floor' || word === 'ceil' ||
        word === 'now' || word === 'today' || word === 'year' || word === 'month' || word === 'day' ||
        word === 'isNull' || word === 'default' || word === 'coalesce') {
        return { type: TOKEN_ID, value: word };
      }

      return { type: TOKEN_ID, value: word };
    }

    if (c === '!' && p + 1 < s.length && s[p + 1] === '=') { this.pos = p + 2; return { type: TOKEN_OP, value: '!=' }; }
    if (c === '=' && p + 1 < s.length && s[p + 1] === '=') { this.pos = p + 2; return { type: TOKEN_OP, value: '==' }; }
    if (c === '>' && p + 1 < s.length && s[p + 1] === '=') { this.pos = p + 2; return { type: TOKEN_OP, value: '>=' }; }
    if (c === '<' && p + 1 < s.length && s[p + 1] === '=') { this.pos = p + 2; return { type: TOKEN_OP, value: '<=' }; }
    if (c === '&' && p + 1 < s.length && s[p + 1] === '&') { this.pos = p + 2; return { type: TOKEN_OP, value: '&&' }; }
    if (c === '|' && p + 1 < s.length && s[p + 1] === '|') { this.pos = p + 2; return { type: TOKEN_OP, value: '||' }; }
    if (c === '>' || c === '<' || c === '!') { this.pos = p + 1; return { type: TOKEN_OP, value: c }; }

    this.pos = p + 1;
    return { type: TOKEN_OP, value: c };
  };

  function Parser(input) {
    this.lexer = new Lexer(input);
    this.current = this.lexer.next();
  }

  Parser.prototype.advance = function () { this.current = this.lexer.next(); };

  Parser.prototype.expect = function (type, msg) {
    if (this.current.type !== type) throw new Error(msg || 'unexpected token');
  };

  Parser.prototype.parse = function () {
    var result = this.parseTernary();
    this.expect(TOKEN_EOF, 'unexpected token after expression');
    return result;
  };

  Parser.prototype.parseTernary = function () {
    var cond = this.parseOr();
    if (this.current.type === TOKEN_QMARK) {
      this.advance();
      var thenExpr = this.parseTernary();
      this.expect(TOKEN_COLON, "expected ':' in ternary");
      this.advance();
      var elseExpr = this.parseTernary();
      return { type: 'ternary', cond: cond, thenExpr: thenExpr, elseExpr: elseExpr };
    }
    return cond;
  };

  Parser.prototype.parseOr = function () {
    var left = this.parseAnd();
    while (this.current.type === TOKEN_OP && this.current.value === '||') {
      this.advance();
      left = { type: 'binary', op: '||', left: left, right: this.parseAnd() };
    }
    return left;
  };

  Parser.prototype.parseAnd = function () {
    var left = this.parseEquality();
    while (this.current.type === TOKEN_OP && this.current.value === '&&') {
      this.advance();
      left = { type: 'binary', op: '&&', left: left, right: this.parseEquality() };
    }
    return left;
  };

  Parser.prototype.parseEquality = function () {
    var left = this.parseCompare();
    while (this.current.type === TOKEN_OP && (this.current.value === '==' || this.current.value === '!=')) {
      var op = this.current.value;
      this.advance();
      left = { type: 'binary', op: op, left: left, right: this.parseCompare() };
    }
    return left;
  };

  Parser.prototype.parseCompare = function () {
    var left = this.parseContains();
    while (this.current.type === TOKEN_OP && (this.current.value === '>' || this.current.value === '<' || this.current.value === '>=' || this.current.value === '<=')) {
      var op2 = this.current.value;
      this.advance();
      left = { type: 'binary', op: op2, left: left, right: this.parseContains() };
    }
    return left;
  };

  Parser.prototype.parseContains = function () {
    var left = this.parseEmpty();
    while (this.current.type === TOKEN_ID && this.current.value === 'contains') {
      this.advance();
      left = { type: 'binary', op: 'contains', left: left, right: this.parseEmpty() };
    }
    return left;
  };

  Parser.prototype.parseEmpty = function () {
    if (this.current.type === TOKEN_ID && this.current.value === 'empty') {
      this.advance();
      var operand = this.parseAtom();
      return { type: 'unary', op: 'empty', operand: operand };
    }
    return this.parseUnary();
  };

  Parser.prototype.parseUnary = function () {
    if (this.current.type === TOKEN_OP && this.current.value === '!') {
      this.advance();
      return { type: 'unary', op: '!', operand: this.parseUnary() };
    }
    if (this.current.type === TOKEN_OP && this.current.value === '-') {
      this.advance();
      return { type: 'unary', op: '-', operand: this.parseUnary() };
    }
    return this.parseAtom();
  };

  Parser.prototype.parseAtom = function () {
    var tok = this.current;

    if (tok.type === TOKEN_LPAREN) {
      this.advance();
      var expr = this.parseTernary();
      this.expect(TOKEN_RPAREN, "expected ')'");
      this.advance();
      return expr;
    }

    if (tok.type === TOKEN_ID) {
      this.advance();
      if (tok.value === 'true') return { type: 'literal', value: true };
      if (tok.value === 'false') return { type: 'literal', value: false };
      if (tok.value === 'null') return { type: 'literal', value: null };

      // Check for function call
      if (this.current.type === TOKEN_LPAREN) {
        return this.parseFunctionCall(tok.value);
      }

      return { type: 'field', name: tok.value };
    }

    if (tok.type === TOKEN_NUM) { this.advance(); return { type: 'literal', value: tok.value }; }
    if (tok.type === TOKEN_STR) { this.advance(); return { type: 'literal', value: tok.value }; }

    throw new Error('unexpected token: ' + JSON.stringify(tok));
  };

  Parser.prototype.parseFunctionCall = function (name) {
    this.expect(TOKEN_LPAREN, "expected '(' after function name");
    this.advance();

    var args = [];
    if (this.current.type !== TOKEN_RPAREN) {
      args.push(this.parseTernary());
      while (this.current.type === TOKEN_COMMA) {
        this.advance();
        args.push(this.parseTernary());
      }
    }

    this.expect(TOKEN_RPAREN, "expected ')' after function arguments");
    this.advance();

    return { type: 'call', name: name, args: args };
  };

  function evaluate(ast, lookup) {
    if (!ast) return null;

    switch (ast.type) {
      case 'literal': return ast.value;
      case 'field': return lookup(ast.name);

      case 'unary':
        var val = evaluate(ast.operand, lookup);
        if (ast.op === '!') return !toBool(val);
        if (ast.op === '-') return -num(val);
        if (ast.op === 'empty') return val == null || val === '' || val === false;
        return val;

      case 'binary': {
        var l = evaluate(ast.left, lookup);
        var r = evaluate(ast.right, lookup);

        switch (ast.op) {
          case '||': return toBool(l) || toBool(r);
          case '&&': return toBool(l) && toBool(r);
          case '==': return l == r;
          case '!=': return l != r;
          case '>': return num(l) > num(r);
          case '<': return num(l) < num(r);
          case '>=': return num(l) >= num(r);
          case '<=': return num(l) <= num(r);
          case 'contains':
            var ls = l != null ? String(l) : '';
            var rs = r != null ? String(r) : '';
            return ls.indexOf(rs) >= 0;
        }
        break;
      }

      case 'ternary': {
        var cond = evaluate(ast.cond, lookup);
        return toBool(cond) ? evaluate(ast.thenExpr, lookup) : evaluate(ast.elseExpr, lookup);
      }

      case 'call': {
        var args = ast.args.map(function (arg) { return evaluate(arg, lookup); });
        return evaluateFunction(ast.name, args);
      }
    }

    return null;
  }

  function evaluateFunction(name, args) {
    var val = args[0];
    var str = val != null ? String(val) : '';
    var n = num(val);

    switch (name) {
      // String functions
      case 'contains': return str.indexOf(args[1] != null ? String(args[1]) : '') >= 0;
      case 'empty': return val == null || val === '' || val === false;
      case 'length': return str.length;
      case 'trim': return str.trim();
      case 'lower': return str.toLowerCase();
      case 'upper': return str.toUpperCase();
      case 'substring': {
        var start = num(args[1]);
        var end = args[2] != null ? num(args[2]) : str.length;
        return str.substring(start, end);
      }
      case 'startsWith': return str.startsWith(args[1] != null ? String(args[1]) : '');
      case 'endsWith': return str.endsWith(args[1] != null ? String(args[1]) : '');
      case 'indexOf': return str.indexOf(args[1] != null ? String(args[1]) : '');

      // Math functions
      case 'abs': return Math.abs(n);
      case 'min': return Math.min.apply(Math, args.map(num));
      case 'max': return Math.max.apply(Math, args.map(num));
      case 'round': return Math.round(n);
      case 'floor': return Math.floor(n);
      case 'ceil': return Math.ceil(n);

      // Date functions
      case 'now': return Date.now();
      case 'today': {
        var d = new Date();
        d.setHours(0, 0, 0, 0);
        return d.getTime();
      }
      case 'year': {
        var date = val ? new Date(val) : new Date();
        return date.getFullYear();
      }
      case 'month': {
        var date2 = val ? new Date(val) : new Date();
        return date2.getMonth() + 1;
      }
      case 'day': {
        var date3 = val ? new Date(val) : new Date();
        return date3.getDate();
      }

      // Utility functions
      case 'isNull': return val == null;
      case 'default': return val != null ? val : (args[1] != null ? args[1] : '');
      case 'coalesce': {
        for (var i = 0; i < args.length; i++) {
          if (args[i] != null) return args[i];
        }
        return null;
      }
    }

    return null;
  }

  function toBool(val) { return val !== null && val !== undefined && val !== false && val !== 0 && val !== ''; }
  function num(val) { var n = parseFloat(val); return isNaN(n) ? 0 : n; }
  function selector(name, value) {
    var escaped = window.CSS && typeof window.CSS.escape === 'function'
      ? window.CSS.escape(String(value))
      : String(value).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    return '[' + name + '="' + escaped + '"]';
  }

  function parseExpression(expr) {
    if (!expr) return null;
    try {
      return new Parser(expr).parse();
    } catch (e) {
      return null;
    }
  }

  var _exprCache = {};

  function getAst(expr) {
    if (!_exprCache[expr]) {
      _exprCache[expr] = parseExpression(expr);
    }
    return _exprCache[expr];
  }

  function isExpr(value) {
    if (!value || typeof value !== 'string') return false;
    var trimmed = value.trim();
    if (/[><!=&|?]/.test(trimmed) || /\bcontains\b|\bempty\b/.test(trimmed)) return true;
    return !!getAst(trimmed) && /\b(length|trim|lower|upper|substring|startsWith|endsWith|indexOf|abs|min|max|round|floor|ceil|now|today|year|month|day|isNull|default|coalesce)\s*\(/.test(trimmed);
  }

  function readFieldValue(form, name) {
    var el = form.querySelector(selector('name', name) + ',' + selector('data-cs-bind', name));
    if (el && 'value' in el) return el.value;
    if (el) return el.textContent;
    return '';
  }

  function evaluateExpr(expr, form) {
    var ast = getAst(expr);
    if (!ast) return null;
    return evaluate(ast, function (name) { return readFieldValue(form, name); });
  }

  function applyShowExpression(root) {
    root.querySelectorAll('[data-cs-show]').forEach(function (el) {
      var expr = el.getAttribute('data-cs-show');
      if (!isExpr(expr)) return;

      var result = evaluateExpr(expr, root);
      if (result == null && result !== false) result = false;
      el.style.display = result ? '' : 'none';
    });
  }

  function applyEnableExpression(root) {
    root.querySelectorAll('[data-cs-enable]').forEach(function (el) {
      var expr = el.getAttribute('data-cs-enable');
      if (!isExpr(expr)) return;

      var result = evaluateExpr(expr, root);
      if ('disabled' in el) el.disabled = !result;
    });
  }

  function applyExpressions(root) {
    applyShowExpression(root);
    applyEnableExpression(root);
    applySourceOptions(root);
  }

  function applySourceOptions(root) {
    root.querySelectorAll('select[data-cs-source]').forEach(function (select) {
      if (select.getAttribute('data-cs-source-ready') === 'true' && select.getAttribute('data-cs-source-reload') !== 'true') return;

      var command = select.getAttribute('data-cs-source');
      if (!command) return;

      var form = select.closest('[data-cs-vm]');
      if (!form) return;

      select.setAttribute('data-cs-source-ready', 'true');
      CodeSpirit.vm(form).invoke(command).then(function (result) {
        var key = select.getAttribute('data-cs-source-field') || (select.name ? select.name + 'Options' : command);
        var options = (result && result.state && result.state[key]) || (result && result[key]) || (result && result.options) || [];
        renderOptions(select, options);
      }).catch(function () {
        select.removeAttribute('data-cs-source-ready');
      });
    });
  }

  function renderOptions(select, options) {
    if (!Array.isArray(options)) return;

    var current = select.value;
    if (typeof select.replaceChildren === 'function') {
      select.replaceChildren();
    } else {
      while (select.firstChild) {
        select.removeChild(select.firstChild);
      }
    }

    options.forEach(function (item) {
      var option = document.createElement('option');
      var value = item;
      var text = item;
      if (item && typeof item === 'object') {
        value = item.value != null ? item.value : (item.Value != null ? item.Value : (item.id != null ? item.id : item.Id));
        text = item.text != null ? item.text : (item.Text != null ? item.Text : (item.label != null ? item.label : (item.Label != null ? item.Label : (item.name != null ? item.name : item.Name))));
      }
      option.value = value == null ? '' : String(value);
      option.textContent = text == null ? option.value : String(text);
      select.appendChild(option);
    });

    if (current) {
      select.value = current;
    }
  }

  function hookUpdateField() {
    var _originalUpdateField = CodeSpirit.updateField;
    CodeSpirit.updateField = function (root, name, value) {
      _originalUpdateField(root, name, value);
      applyExpressions(root);
    };
  }

  function refreshElementExpr(root) {
    var form = root && root.matches && root.matches('[data-cs-vm]') ? root : (root && root.closest ? root.closest('[data-cs-vm]') : null);
    if (form) applyExpressions(form);
  }

  function handleRefresh(event) {
    var field = event.target;
    if (!field || !field.getAttribute) return;

    var regionName = field.getAttribute('data-cs-refresh');
    if (!regionName) return;

    var form = field.closest('[data-cs-vm]');
    if (!form) return;

    var debounce = field.getAttribute('data-cs-refresh-debounce') || field.getAttribute('data-cs-debounce');
    var delay = debounce ? parseInt(debounce, 10) || 300 : 300;
    var key = '__cs_refresh_timer_' + regionName;
    if (form[key]) clearTimeout(form[key]);

    form[key] = setTimeout(function () {
      try { CodeSpirit.vm(form).invoke(); } catch (_) {}
    }, delay);
  }

  function handleConfirm(event) {
    var el = event.target && event.target.closest ? event.target.closest('[data-cs-confirm]') : null;
    if (!el) return;

    var message = el.getAttribute('data-cs-confirm') || 'Are you sure?';
    if (typeof window.confirm === 'function' && !window.confirm(message)) {
      event.preventDefault();
      if (typeof event.stopImmediatePropagation === 'function') {
        event.stopImmediatePropagation();
      }
    }
  }

  document.addEventListener('codespirit:updated', function () {
    document.querySelectorAll('[data-cs-vm]').forEach(applyExpressions);
  });

  document.addEventListener('codespirit:changed', function (event) {
    refreshElementExpr(event.target);
  });

  document.addEventListener('input', handleRefresh);
  document.addEventListener('codespirit:input', handleRefresh);
  document.addEventListener('click', handleConfirm, true);

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-cs-vm]').forEach(applyExpressions);
  });

  hookUpdateField();

  CodeSpirit.expression = {
    evaluate: evaluateExpr,
    parse: parseExpression,
    isExpr: isExpr,
    apply: applyExpressions
  };
})();

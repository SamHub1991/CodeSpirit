(function () {
  'use strict';

  var analyzers = {};

  var SCENE_DEFINITIONS = [
    {
      name: 'dashboard',
      threshold: 7,
      keywords: [
        ['大屏', 8], ['看板', 7], ['驾驶舱', 7], ['dashboard', 7], ['screen', 4],
        ['kpi', 4], ['metric', 4], ['metrics', 4], ['chart', 4], ['trend', 3],
        ['forecast', 3], ['overview', 2], ['realtime', 4], ['real-time', 4]
      ]
    },
    {
      name: 'library',
      threshold: 7,
      keywords: [
        ['图书', 8], ['图书馆', 8], ['library', 8], ['book', 5], ['books', 5],
        ['catalog', 5], ['reader', 4], ['readers', 4], ['borrow', 4], ['loan', 4],
        ['loans', 4], ['isbn', 5], ['reservation', 4], ['reservations', 4], ['fine', 3]
      ]
    },
    {
      name: 'admin',
      threshold: 7,
      keywords: [
        ['管理', 7], ['后台', 7], ['admin', 7], ['management', 5], ['manage', 4],
        ['crud', 4], ['filter', 3], ['search', 3], ['status', 3], ['table', 3],
        ['create', 2], ['update', 2], ['archive', 3], ['restore', 3]
      ]
    },
    {
      name: 'commerce',
      threshold: 7,
      keywords: [
        ['电商', 8], ['商城', 8], ['订单', 7], ['商品', 7], ['commerce', 7],
        ['shop', 5], ['order', 5], ['orders', 5], ['product', 4], ['products', 4],
        ['cart', 4], ['payment', 4], ['price', 3], ['inventory', 3]
      ]
    },
    {
      name: 'content',
      threshold: 7,
      keywords: [
        ['内容', 7], ['文章', 7], ['博客', 7], ['cms', 7], ['content', 6],
        ['article', 5], ['articles', 5], ['post', 4], ['posts', 4], ['author', 3],
        ['category', 3], ['publish', 4], ['draft', 3]
      ]
    },
    {
      name: 'analytics',
      threshold: 7,
      keywords: [
        ['报表', 8], ['分析', 7], ['统计', 7], ['analytics', 7], ['report', 5],
        ['reports', 5], ['analysis', 5], ['statistics', 5], ['conversion', 4], ['rate', 3],
        ['growth', 3], ['segment', 3]
      ]
    },
    {
      name: 'crm',
      threshold: 7,
      keywords: [
        ['客户', 8], ['线索', 7], ['商机', 7], ['crm', 8], ['customer', 5],
        ['customers', 5], ['lead', 5], ['leads', 5], ['opportunity', 5], ['pipeline', 4],
        ['deal', 4], ['deals', 4], ['contact', 4], ['contacts', 4]
      ]
    },
    {
      name: 'finance',
      threshold: 7,
      keywords: [
        ['财务', 8], ['账单', 7], ['发票', 7], ['支付', 6], ['finance', 7],
        ['invoice', 5], ['invoices', 5], ['billing', 5], ['revenue', 5], ['expense', 4],
        ['expenses', 4], ['budget', 4], ['balance', 4], ['amount', 3]
      ]
    },
    {
      name: 'education',
      threshold: 7,
      keywords: [
        ['教育', 8], ['课程', 7], ['学生', 7], ['考试', 6], ['education', 7],
        ['course', 5], ['courses', 5], ['student', 5], ['students', 5], ['class', 4],
        ['lesson', 4], ['lessons', 4], ['grade', 4], ['exam', 4]
      ]
    },
    {
      name: 'healthcare',
      threshold: 7,
      keywords: [
        ['医疗', 8], ['医院', 8], ['患者', 7], ['医生', 6], ['healthcare', 7],
        ['medical', 6], ['patient', 5], ['patients', 5], ['doctor', 5], ['doctors', 5],
        ['appointment', 4], ['clinic', 4], ['diagnosis', 4], ['prescription', 4]
      ]
    },
    {
      name: 'logistics',
      threshold: 7,
      keywords: [
        ['物流', 8], ['仓储', 7], ['配送', 7], ['运单', 7], ['logistics', 7],
        ['shipment', 5], ['shipments', 5], ['delivery', 5], ['warehouse', 5], ['tracking', 4],
        ['fleet', 4], ['route', 4], ['routes', 4], ['package', 4]
      ]
    },
    {
      name: 'developer',
      threshold: 7,
      keywords: [
        ['开发者', 8], ['接口', 6], ['日志', 5], ['developer', 7], ['api', 5],
        ['token', 4], ['webhook', 5], ['sdk', 5], ['deploy', 4], ['deployment', 4],
        ['build', 4], ['log', 4], ['logs', 4], ['repository', 4]
      ]
    },
    {
      name: 'hr',
      threshold: 7,
      keywords: [
        ['人事', 8], ['招聘', 7], ['员工', 7], ['考勤', 6], ['hr', 7],
        ['employee', 5], ['employees', 5], ['recruit', 5], ['recruiting', 5], ['candidate', 5],
        ['payroll', 5], ['attendance', 4], ['leave', 4], ['onboarding', 4]
      ]
    },
    {
      name: 'manufacturing',
      threshold: 7,
      keywords: [
        ['制造', 8], ['生产', 7], ['工单', 6], ['产线', 6], ['manufacturing', 7],
        ['production', 5], ['workorder', 5], ['work order', 5], ['factory', 5], ['machine', 4],
        ['assembly', 4], ['quality', 4], ['defect', 4], ['oee', 5]
      ]
    },
    {
      name: 'hospitality',
      threshold: 7,
      keywords: [
        ['酒店', 8], ['房间', 6], ['入住', 6], ['预订', 5], ['hospitality', 7],
        ['hotel', 6], ['room', 5], ['rooms', 5], ['guest', 5], ['guests', 5],
        ['booking', 5], ['reservation', 4], ['check-in', 4], ['checkout', 4]
      ]
    },
    {
      name: 'real-estate',
      threshold: 7,
      keywords: [
        ['房产', 8], ['楼盘', 7], ['租赁', 6], ['物业', 6], ['real estate', 7],
        ['property', 5], ['properties', 5], ['listing', 5], ['lease', 5], ['tenant', 5],
        ['rent', 4], ['mortgage', 4], ['apartment', 4], ['building', 4]
      ]
    },
    {
      name: 'legal',
      threshold: 7,
      keywords: [
        ['法务', 8], ['合同', 7], ['案件', 7], ['合规', 6], ['legal', 7],
        ['contract', 5], ['contracts', 5], ['case', 5], ['cases', 5], ['compliance', 5],
        ['clause', 4], ['review', 3], ['risk', 4], ['policy', 4]
      ]
    },
    {
      name: 'support',
      threshold: 7,
      keywords: [
        ['客服', 8], ['工单', 6], ['服务台', 7], ['支持', 5], ['support', 7],
        ['ticket', 5], ['tickets', 5], ['helpdesk', 6], ['agent', 4], ['sla', 5],
        ['incident', 5], ['request', 3], ['priority', 4], ['queue', 4]
      ]
    },
    {
      name: 'supply-chain',
      threshold: 7,
      keywords: [
        ['供应链', 8], ['采购', 7], ['供应商', 7], ['仓储', 6], ['supply chain', 7],
        ['supply-chain', 7], ['procurement', 5], ['vendor', 5], ['vendors', 5],
        ['supplier', 5], ['suppliers', 5], ['sourcing', 4], ['purchase order', 4],
        ['shipment', 4], ['logistics', 4], ['inventory', 4]
      ]
    },
    {
      name: 'research',
      threshold: 7,
      keywords: [
        ['科研', 8], ['研究', 7], ['实验室', 7], ['实验', 6], ['research', 7],
        ['lab', 5], ['laboratory', 5], ['experiment', 5], ['experiments', 5],
        ['study', 4], ['studies', 4], ['publication', 4], ['paper', 4],
        ['trial', 4], ['protocol', 3], ['sample', 3]
      ]
    },
    {
      name: 'security',
      threshold: 7,
      keywords: [
        ['安全', 8], ['防护', 7], ['漏洞', 7], ['审计', 6], ['security', 7],
        ['audit', 5], ['vulnerability', 5], ['vulnerabilities', 5], ['threat', 5],
        ['intrusion', 4], ['firewall', 4], ['compliance', 4], ['permission', 4],
        ['encryption', 4], ['incident', 4]
      ]
    },
    {
      name: 'retail',
      threshold: 7,
      keywords: [
        ['零售', 8], ['门店', 7], ['收银', 6], ['促销', 6], ['retail', 7],
        ['store', 5], ['stores', 5], ['pos', 5], ['checkout', 5],
        ['customer', 4], ['membership', 4], ['loyalty', 4], ['promotion', 4],
        ['discount', 4], ['shelf', 3]
      ]
    },
    {
      name: 'insurance',
      threshold: 7,
      keywords: [
        ['保险', 8], ['保单', 7], ['理赔', 7], ['投保', 6], ['insurance', 7],
        ['policy', 5], ['policies', 5], ['claim', 5], ['claims', 5],
        ['premium', 5], ['underwriting', 5], ['coverage', 4], ['beneficiary', 4],
        ['deductible', 3]
      ]
    },
    {
      name: 'ngo',
      threshold: 7,
      keywords: [
        ['公益', 8], ['慈善', 7], ['捐赠', 7], ['志愿者', 6], ['ngo', 7],
        ['nonprofit', 6], ['charity', 5], ['donation', 5], ['donations', 5],
        ['grant', 5], ['grants', 5], ['volunteer', 5], ['fundraiser', 4],
        ['community', 3], ['outreach', 3]
      ]
    },
    {
      name: 'telecom',
      threshold: 7,
      keywords: [
        ['电信', 8], ['通信', 7], ['网络', 6], ['基站', 6], ['telecom', 7],
        ['network', 5], ['signal', 5], ['tower', 5], ['bts', 5],
        ['subscriber', 4], ['sim', 4], ['plan', 3], ['roaming', 4]
      ]
    },
    {
      name: 'energy',
      threshold: 7,
      keywords: [
        ['能源', 8], ['电力', 7], ['电网', 7], ['发电', 6], ['energy', 7],
        ['power', 5], ['grid', 5], ['electricity', 5], ['renewable', 4],
        ['solar', 4], ['wind', 4], ['consumption', 4], ['utility', 4]
      ]
    },
    {
      name: 'transportation',
      threshold: 7,
      keywords: [
        ['交通', 8], ['运输', 7], ['公交', 7], ['地铁', 7], ['transportation', 7],
        ['transit', 5], ['metro', 5], ['bus', 5], ['train', 5],
        ['route', 4], ['schedule', 4], ['ticket', 4], ['fare', 3]
      ]
    },
    {
      name: 'agriculture',
      threshold: 7,
      keywords: [
        ['农业', 8], ['农场', 7], ['种植', 7], ['农田', 6], ['agriculture', 7],
        ['farm', 5], ['crop', 5], ['harvest', 5], ['irrigation', 4],
        ['soil', 4], ['fertilizer', 4], ['pesticide', 4], ['yield', 4]
      ]
    },
    {
      name: 'media',
      threshold: 7,
      keywords: [
        ['媒体', 8], ['新闻', 7], ['广播', 7], ['视频', 6], ['media', 7],
        ['news', 5], ['broadcast', 5], ['video', 5], ['streaming', 5],
        ['channel', 4], ['program', 4], ['episode', 4], ['viewer', 3]
      ]
    },
    {
      name: 'gaming',
      threshold: 7,
      keywords: [
        ['游戏', 8], ['玩家', 7], ['电竞', 7], ['服务器', 6], ['gaming', 7],
        ['game', 5], ['player', 5], ['esports', 5], ['server', 5],
        ['match', 4], ['rank', 4], ['level', 4], ['score', 3]
      ]
    },
    {
      name: 'automotive',
      threshold: 7,
      keywords: [
        ['汽车', 8], ['车辆', 7], ['4s 店', 7], ['维修', 6], ['automotive', 7],
        ['vehicle', 5], ['car', 5], ['dealer', 5], ['service', 5],
        ['maintenance', 4], ['repair', 4], ['warranty', 4], ['parts', 4]
      ]
    },
    {
      name: 'pharmaceutical',
      threshold: 7,
      keywords: [
        ['医药', 8], ['药品', 7], ['制药', 7], ['药房', 6], ['pharmaceutical', 7],
        ['drug', 5], ['medicine', 5], ['medication', 5], ['pharmacy', 5],
        ['prescription', 4], ['dosage', 4], ['tablet', 4], ['capsule', 4]
      ]
    },
    {
      name: 'construction',
      threshold: 7,
      keywords: [
        ['建筑', 8], ['工程', 7], ['施工', 7], ['工地', 6], ['construction', 7],
        ['building', 5], ['project', 5], ['site', 5], ['contractor', 5],
        ['blueprint', 4], ['material', 4], ['concrete', 4], ['steel', 4]
      ]
    },
    {
      name: 'aviation',
      threshold: 7,
      keywords: [
        ['航空', 8], ['航班', 7], ['机场', 7], ['飞机', 6], ['aviation', 7],
        ['flight', 5], ['airport', 5], ['airline', 5], ['aircraft', 5],
        ['boarding', 4], ['gate', 4], ['terminal', 4], ['pilot', 4]
      ]
    },
    {
      name: 'maritime',
      threshold: 7,
      keywords: [
        ['海运', 8], ['港口', 7], ['船舶', 7], ['码头', 6], ['maritime', 7],
        ['shipping', 5], ['port', 5], ['vessel', 5], ['cargo', 5],
        ['container', 4], ['dock', 4], ['harbor', 4], ['freight', 4]
      ]
    },
    {
      name: 'government',
      threshold: 7,
      keywords: [
        ['政府', 8], ['政务', 7], ['行政', 7], ['公务', 6], ['government', 7],
        ['agency', 5], ['department', 5], ['bureau', 5], ['civil', 5],
        ['public', 4], ['service', 4], ['permit', 4], ['license', 4]
      ]
    }
  ];

  var THEME_TOKEN_NAMES = [
    '--cs-primary', '--cs-primary-dark', '--cs-accent', '--cs-accent-2', '--cs-bg',
    '--cs-surface', '--cs-surface-strong', '--cs-text', '--cs-text-muted', '--cs-text-soft',
    '--cs-text-on-primary', '--cs-border', '--cs-border-soft', '--cs-header-bg', '--cs-radius',
    '--cs-radius-lg', '--cs-shadow', '--cs-shadow-soft', '--cs-ring', '--cs-font',
    '--cs-tone-green', '--cs-tone-red', '--cs-tone-amber', '--cs-tone-blue', '--cs-tone-purple'
  ];

  var DEFAULT_THEME_TOKENS = {
    '--cs-primary': '#6d5dfc',
    '--cs-primary-dark': '#4f46e5',
    '--cs-accent': '#00d4ff',
    '--cs-accent-2': '#a7f3d0',
    '--cs-bg': '#f4f7fb',
    '--cs-surface': 'rgba(255, 255, 255, 0.86)',
    '--cs-surface-strong': '#ffffff',
    '--cs-text': '#111827',
    '--cs-text-muted': '#5b677a',
    '--cs-text-soft': '#93a0b3',
    '--cs-text-on-primary': '#ffffff',
    '--cs-border': 'rgba(119, 134, 159, 0.22)',
    '--cs-border-soft': 'rgba(119, 134, 159, 0.12)',
    '--cs-header-bg': 'rgba(255, 255, 255, 0.72)',
    '--cs-radius': '18px',
    '--cs-radius-lg': '28px',
    '--cs-shadow': '0 20px 60px rgba(17, 24, 39, 0.10)',
    '--cs-shadow-soft': '0 12px 36px rgba(17, 24, 39, 0.07)',
    '--cs-ring': '0 0 0 4px rgba(109, 93, 252, 0.16)',
    '--cs-font': "Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', sans-serif",
    '--cs-tone-green': '#22c55e',
    '--cs-tone-red': '#ef4444',
    '--cs-tone-amber': '#f59e0b',
    '--cs-tone-blue': '#3b82f6',
    '--cs-tone-purple': '#a855f7'
  };

  var SCENE_THEME_VARIANTS = {
    dashboard: {
      '--cs-primary': '#7c3aed',
      '--cs-accent': '#f59e0b',
      '--cs-bg': '#0f172a'
    },
    finance: {
      '--cs-primary': '#059669',
      '--cs-accent': '#10b981',
      '--cs-text': '#064e3b'
    },
    healthcare: {
      '--cs-primary': '#0891b2',
      '--cs-accent': '#06b6d4',
      '--cs-bg': '#ecfeff'
    },
    education: {
      '--cs-primary': '#ea580c',
      '--cs-accent': '#f97316',
      '--cs-bg': '#fff7ed'
    },
    gaming: {
      '--cs-primary': '#dc2626',
      '--cs-accent': '#ef4444',
      '--cs-bg': '#18181b'
    },
    energy: {
      '--cs-primary': '#84cc16',
      '--cs-accent': '#a3e635',
      '--cs-bg': '#f7fee7'
    },
    telecom: {
      '--cs-primary': '#2563eb',
      '--cs-accent': '#3b82f6',
      '--cs-bg': '#eff6ff'
    },
    government: {
      '--cs-primary': '#1e40af',
      '--cs-accent': '#3730a3',
      '--cs-text': '#1e3a8a'
    }
  };

  function getTextValue(el) {
    if (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA') {
      return el.value;
    }
    return (el.textContent || '').trim();
  }

  function getNumericValue(el) {
    var raw = getTextValue(el);
    if (!raw) return NaN;
    var cleaned = raw.replace(/[^\d.\-+]/g, '');
    var num = parseFloat(cleaned);
    return isNaN(num) ? NaN : num;
  }

  function clearIntentClasses(el) {
    var toRemove = [];
    var list = el.classList;
    for (var i = 0; i < list.length; i++) {
      if (list[i].startsWith('intent-')) {
        toRemove.push(list[i]);
      }
    }
    toRemove.forEach(function (c) { el.classList.remove(c); });
  }

  function applyIntentTone(el, tone) {
    if (!tone || tone === 'default') return;

    clearIntentClasses(el);
    el.classList.add('intent-' + tone);

    if (el.hasAttribute('data-cs-tone')) return;

    var baseClass = el.classList[0] || '';
    if (!baseClass) return;

    var prefixDash = baseClass + '-';
    Array.from(el.classList).forEach(function (c) {
      if (c !== baseClass && c.startsWith(prefixDash)) {
        el.classList.remove(c);
      }
    });

    el.classList.add(baseClass + '-' + tone);
  }

  function normalizeSceneName(value) {
    var scene = String(value || '').trim().toLowerCase().replace(/[^a-z0-9_-]+/g, '-');
    return scene.replace(/^-+|-+$/g, '');
  }

  function clearSceneClasses(el) {
    var toRemove = [];
    Array.from(el.classList || []).forEach(function (className) {
      if (className.indexOf('cs-scene-') === 0) toRemove.push(className);
    });
    toRemove.forEach(function (className) { el.classList.remove(className); });
  }

  function getSceneTarget(root) {
    if (root && root.nodeType === 1) return root;
    return document.body || document.documentElement || (root && root.querySelector && root.querySelector('[data-cs-vm]')) || (root && root.firstElementChild) || root;
  }

  function collectSceneText(root) {
    var parts = [];
    var source = root || document;
    var title = document.title || '';
    var path = (window.location && window.location.pathname) || '';
    if (title) parts.push(title);
    if (path) parts.push(path);

    if (source.querySelectorAll) {
      Array.from(source.querySelectorAll('h1,h2,h3,th,label,button,a,input,textarea,select,[class],[data-cs-scene]')).forEach(function (el) {
        parts.push(el.textContent || '');
        parts.push(el.getAttribute('class') || '');
        parts.push(el.getAttribute('name') || '');
        parts.push(el.getAttribute('placeholder') || '');
        parts.push(el.getAttribute('data-cs-scene') || '');
        parts.push(el.getAttribute('data-cs-command') || '');
      });
    }

    return parts.join(' ').toLowerCase();
  }

  function scoreScene(text, definition) {
    return definition.keywords.reduce(function (score, item) {
      return text.indexOf(item[0]) >= 0 ? score + item[1] : score;
    }, 0);
  }

  function inferScene(root) {
    var target = getSceneTarget(root);
    if (!target || !target.classList) return null;

    var explicit = target.getAttribute && target.getAttribute('data-cs-scene');
    if (!explicit && root && root.querySelector) {
      var explicitEl = root.querySelector('[data-cs-scene]');
      explicit = explicitEl && explicitEl.getAttribute('data-cs-scene');
    }

    if (explicit) {
      return normalizeSceneName(explicit.split(/\s+/)[0]);
    }

    var text = collectSceneText(root);
    var best = { name: 'app', score: 0, threshold: 1 };
    SCENE_DEFINITIONS.forEach(function (definition) {
      var score = scoreScene(text, definition);
      if (score > best.score) best = { name: definition.name, score: score, threshold: definition.threshold };
    });

    if (best.score > 0 && best.score < best.threshold && target.setAttribute) {
      target.setAttribute('data-cs-scene-confidence', 'low');
      target.setAttribute('data-cs-scene-candidate', best.name);
      target.setAttribute('data-cs-scene-score', String(best.score));
    } else if (target.removeAttribute) {
      target.removeAttribute('data-cs-scene-confidence');
      target.removeAttribute('data-cs-scene-candidate');
      target.removeAttribute('data-cs-scene-score');
    }

    return best.score >= best.threshold ? best.name : 'app';
  }

  function applyScene(root) {
    var target = getSceneTarget(root);
    if (!target || !target.classList) return;

    var scene = inferScene(root);
    if (!scene) return;

    clearSceneClasses(target);
    target.classList.add('cs-scene-' + scene);
    if (target.setAttribute) target.setAttribute('data-cs-scene-current', scene);
  }

  // ============================
  // numeric — tier by distribution
  // ============================
  analyzers.numeric = function (elements, root) {
    var pairs = [];
    elements.forEach(function (el) {
      var num = getNumericValue(el);
      if (!isNaN(num)) pairs.push({ el: el, val: num });
    });

    if (pairs.length === 0) return;

    var sorted = pairs.map(function (p) { return p.val; }).sort(function (a, b) { return a - b; });
    var len = sorted.length;
    var p30 = sorted[Math.floor(len * 0.30)];
    var p70 = sorted[Math.floor(len * 0.70)];

    if (sorted.length === 1) {
      applyIntentTone(pairs[0].el, pairs[0].val > 0 ? 'success' : pairs[0].val < 0 ? 'danger' : 'muted');
      return;
    }

    pairs.forEach(function (item) {
      var tone;
      if (item.val > p70) tone = 'success';
      else if (item.val > p30) tone = 'warning';
      else if (item.val < 0) tone = 'danger';
      else tone = 'muted';

      applyIntentTone(item.el, tone);
    });
  };

  // ============================
  // status — keyword → tone
  // ============================
  var STATUS_MAP = {
    overdue: 'danger', late: 'danger', error: 'danger', failed: 'danger', cancelled: 'danger',
    blocked: 'danger', rejected: 'danger', expired: 'danger', suspended: 'danger',
    available: 'success', active: 'success', success: 'success', ok: 'success',
    completed: 'success', approved: 'success', verified: 'success', ready: 'success',
    reserved: 'warning', pending: 'warning', waiting: 'warning', processing: 'warning',
    review: 'warning', attention: 'warning', hold: 'warning', flagged: 'warning',
    borrowed: 'info', archived: 'info', inactive: 'info', draft: 'info',
    scheduled: 'info', noted: 'info', seen: 'info',
    逾期: 'danger', 失败: 'danger', 错误: 'danger', 取消: 'danger', 已取消: 'danger',
    封禁: 'danger', 拒绝: 'danger', 过期: 'danger', 停用: 'danger', 挂起: 'danger',
    可用: 'success', 成功: 'success', 完成: 'success', 已完成: 'success', 通过: 'success',
    已批准: 'success', 已验证: 'success', 就绪: 'success', 正常: 'success',
    预约: 'warning', 待处理: 'warning', 等待: 'warning', 处理中: 'warning',
    审核: 'warning', 注意: 'warning', 暂停: 'warning', 标记: 'warning',
    已借出: 'info', 借出: 'info', 归档: 'info', 已归档: 'info', 停用_zh: 'info',
    草稿: 'info', 计划: 'info', 已读: 'info', 已阅: 'info', 未激活: 'info'
  };

  analyzers.status = function (elements, root) {
    elements.forEach(function (el) {
      var raw = getTextValue(el).trim();
      var val = raw.toLowerCase().replace(/[^a-z]/g, '');
      var tone = STATUS_MAP[val] || STATUS_MAP[raw] || 'default';
      applyIntentTone(el, tone);
    });
  };

  // ============================
  // due — days remaining → urgency
  // ============================
  analyzers.due = function (elements, root) {
    var now = Date.now();
    var DAY = 24 * 60 * 60 * 1000;

    elements.forEach(function (el) {
      var val = getTextValue(el);
      if (!val) return;

      var date = new Date(val);
      if (isNaN(date.getTime())) return;

      var daysLeft = Math.ceil((date.getTime() - now) / DAY);
      var tone;
      if (daysLeft < 0) tone = 'danger';
      else if (daysLeft <= 3) tone = 'warning';
      else if (daysLeft <= 14) tone = 'info';
      else tone = 'success';

      applyIntentTone(el, tone);
    });
  };

  // ============================
  // trend — sibling comparison
  // ============================
  analyzers.trend = function (elements, root) {
    var prev = NaN;
    elements.forEach(function (el) {
      var curr = getNumericValue(el);
      if (isNaN(curr)) return;

      var tone;
      if (!isNaN(prev)) {
        if (curr > prev) tone = 'success';
        else if (curr < prev) tone = 'danger';
        else tone = 'muted';
      } else {
        tone = 'info';
      }
      prev = curr;

      applyIntentTone(el, tone);
    });
  };

  // ============================
  // Public API
  // ============================
  function register(name, initializer) {
    if (!name || typeof initializer !== 'function') return;
    if (!/^[A-Za-z0-9_-]+$/.test(name)) {
      throw new Error('Invalid CodeSpirit intent analyzer name: ' + name);
    }
    analyzers[name] = initializer;
  }

  function analyze(root) {
    root = root || document;
    applyScene(root);
    Object.keys(analyzers).forEach(function (name) {
      var selector = '[data-cs-intent~="' + name + '"]';
      var elements = Array.from(root.querySelectorAll(selector));
      if (elements.length) {
        analyzers[name](elements, root);
      }
    });
  }

  function exportThemeTokens(root) {
    var target = getSceneTarget(root) || document.documentElement;
    var styles = typeof window.getComputedStyle === 'function' ? window.getComputedStyle(target) : null;
    var tokens = {};

    // Get current scene
    var currentScene = target.getAttribute('data-cs-scene-current');
    if (!currentScene && target.classList) {
      for (var i = 0; i < target.classList.length; i++) {
        var cls = target.classList[i];
        if (cls.startsWith('cs-scene-')) {
          currentScene = cls.replace('cs-scene-', '');
          break;
        }
      }
    }

    // Apply scene-specific theme variants
    var sceneVariant = currentScene && SCENE_THEME_VARIANTS[currentScene] ? SCENE_THEME_VARIANTS[currentScene] : {};

    THEME_TOKEN_NAMES.forEach(function (name) {
      var value = styles && typeof styles.getPropertyValue === 'function'
        ? styles.getPropertyValue(name).trim()
        : '';

      // Priority: computed style > scene variant > default
      tokens[name] = value || sceneVariant[name] || DEFAULT_THEME_TOKENS[name] || '';
    });

    return tokens;
  }

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.intent = window.CodeSpirit.intent || {};
  window.CodeSpirit.intent.analyze = analyze;
  window.CodeSpirit.intent.register = register;
  window.CodeSpirit.theme = window.CodeSpirit.theme || {};
  window.CodeSpirit.theme.tokens = exportThemeTokens;
  window.CodeSpirit.theme.exportTokens = exportThemeTokens;

  document.addEventListener('DOMContentLoaded', function () {
    analyze(document);
  });

  document.addEventListener('codespirit:updated', function (event) {
    analyze(event.target);
  });
})();

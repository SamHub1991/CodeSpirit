<%@ Page %>

<cs:Content PlaceHolder="Head">
  <style>
    .preview-stage { display: grid; gap: 1.5rem; }
    .preview-hero { display: grid; grid-template-columns: 1.4fr 0.8fr; gap: 1rem; align-items: stretch; }
    .preview-panel { background: #fff; border: 1px solid #e2e8f0; border-radius: 18px; padding: 1.25rem; box-shadow: 0 16px 40px rgba(15, 23, 42, 0.08); }
    .preview-title { margin: 0 0 0.5rem; font-size: clamp(2rem, 5vw, 4rem); letter-spacing: -0.05em; }
    .preview-subtitle { color: #64748b; max-width: 58ch; }
    .preview-toolbar { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-top: 1rem; }
    .preview-button { border: 0; border-radius: 999px; padding: 0.75rem 1rem; background: #0f172a; color: #fff; font-weight: 800; cursor: pointer; }
    .preview-button.secondary { background: #e0f2fe; color: #075985; }
    .preview-score { display: grid; place-items: center; min-height: 220px; background: radial-gradient(circle at top, #dbeafe, #fff 64%); }
    .preview-score strong { font-size: 4rem; line-height: 1; }
    .preview-status { display: inline-flex; border-radius: 999px; padding: 0.35rem 0.7rem; background: #dcfce7; color: #166534; font-weight: 800; }
    .preview-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; }
    .preview-card h3 { margin-top: 0; }
    .preview-tree .cs-tree-node { border: 1px solid #e2e8f0; border-radius: 14px; padding: 0.75rem; margin-bottom: 0.5rem; }
    @media (max-width: 900px) { .preview-hero, .preview-grid { grid-template-columns: 1fr; } }
  </style>
</cs:Content>

<cs:Content PlaceHolder="Body">
  <section class="preview-stage" data-cs-intent="dashboard" data-ui="preview-stage">
    <div class="preview-hero">
      <article class="preview-panel" data-cs-tone="Status:preview-status" data-cs-show="Score >= 80">
        <span class="preview-status" data-cs-intent="status">{Binding Status}</span>
        <h1 class="preview-title" data-cs-bind="Title" data-cs-class="Status:is-live:Active">{Binding Title}</h1>
        <p class="preview-subtitle" data-cs-bind="Subtitle">{Binding Subtitle}</p>
        <div class="preview-toolbar">
          <button class="preview-button" data-cs-enable="CanEdit" data-cs-confirm="Apply this preview change?">Primary Action</button>
          <button class="preview-button secondary" data-cs-refresh="preview-region">Refresh Region</button>
          <a class="preview-button secondary" href="/live-preview?dev=1">Open with Dev Panel</a>
        </div>
      </article>

      <aside class="preview-panel preview-score" data-cs-attr="Score:aria-label" data-cs-class="Status:score-live:Active">
        <div>
          <span>UI confidence</span>
          <strong data-cs-intent="numeric">{Binding Score}</strong>
          <p>Edit the style, text, html, class, and data attributes from the Dev Panel.</p>
        </div>
      </aside>
    </div>

    <section class="preview-grid" data-cs-region="preview-region">
      <cs:Repeater Items="{Binding Steps}">
        <article class="preview-panel preview-card" data-ui="clickable-card" data-cs-tone="Key:preview-card">
          <h3>{Binding Title}</h3>
          <p>{Binding Description}</p>
        </article>
      </cs:Repeater>
    </section>

    <cs:Wizard ActiveStep="select" data-ui="guided-preview">
      <cs:Repeater Items="{Binding Steps}">
        <cs:Step Key="{Binding Key}" Title="{Binding Title}">
          <p>{Binding Description}</p>
        </cs:Step>
      </cs:Repeater>
    </cs:Wizard>

    <cs:Tree Items="{Binding Nodes}" TextField="Name" DescriptionField="Description" ToneField="Tone" Collapsed="false" />
  </section>
</cs:Content>

<cs:Content PlaceHolder="Scripts">
  <cs:Script>
    document.addEventListener('codespirit:wizard-step', function (event) {
      document.documentElement.setAttribute('data-preview-step', event.detail.step);
    });
  </cs:Script>
</cs:Content>

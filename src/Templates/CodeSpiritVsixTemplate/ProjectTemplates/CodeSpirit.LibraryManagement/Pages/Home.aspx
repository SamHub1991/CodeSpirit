<%@ Page %>

<!--
  Library dashboard powered by CodeSpirit MVVM
-->
<cs:Content PlaceHolder="Head">
  <link rel="stylesheet" href="/css/pages/home.css" />
</cs:Content>

<cs:Content PlaceHolder="Body">
  <div class="library-screen">
    <section class="screen-hero">
      <div>
        <p class="eyebrow">Live Library Intelligence</p>
        <h1>{Binding WelcomeMessage}</h1>
        <p class="subtitle">{Binding Subtitle}</p>
      </div>
      <a class="admin-entry" href="/admin">Enter Admin</a>
    </section>

    <section class="metric-grid">
      <cs:Repeater Items="{Binding Metrics}">
        <article class="metric-card metric-{Binding Tone}">
          <span>{Binding Label}</span>
          <strong>{Binding Value}</strong>
          <small>{Binding Hint}</small>
        </article>
      </cs:Repeater>
    </section>

    <section class="screen-grid">
      <article class="panel wide-panel">
        <div class="panel-title">
          <h2>Popular Books</h2>
          <span>Monthly activity</span>
        </div>
        <cs:Repeater Items="{Binding PopularBooks}">
          <div class="book-row">
            <div>
              <strong>{Binding Title}</strong>
              <span>{Binding Author} · {Binding Category}</span>
            </div>
            <b>{Binding MonthlyBorrows}</b>
          </div>
        </cs:Repeater>
      </article>

      <article class="panel">
        <div class="panel-title">
          <h2>Category Flow</h2>
          <span>Available copies vs borrowed titles</span>
        </div>
        <cs:Repeater Items="{Binding CategoryStats}">
          <div class="category-row">
            <div>
              <strong>{Binding Name}</strong>
              <span>{Binding Total} books</span>
            </div>
            <em>{Binding Available}/{Binding Borrowed}</em>
          </div>
        </cs:Repeater>
      </article>

      <article class="panel">
        <div class="panel-title">
          <h2>Live Activity</h2>
          <span>Recent circulation</span>
        </div>
        <cs:Repeater Items="{Binding Activities}">
          <div class="activity activity-{Binding Tone}">
            <time>{Binding Time}</time>
            <span>{Binding Text}</span>
          </div>
        </cs:Repeater>
      </article>
    </section>

    <section class="screen-grid compact-grid">
      <article class="panel">
        <div class="panel-title">
          <h2>Active Loans</h2>
          <span>Borrowing and due dates</span>
        </div>
        <cs:Repeater Items="{Binding ActiveLoans}">
          <div class="loan-row status-{Binding Status}">
            <div>
              <strong>{Binding BookTitle}</strong>
              <span>{Binding ReaderName} · due {Binding DueAt}</span>
            </div>
            <em>{Binding Status}</em>
          </div>
        </cs:Repeater>
      </article>

      <article class="panel">
        <div class="panel-title">
          <h2>Reservations</h2>
          <span>Waiting readers</span>
        </div>
        <cs:Repeater Items="{Binding Reservations}">
          <div class="reservation-row">
            <div>
              <strong>{Binding BookTitle}</strong>
              <span>{Binding ReaderName} · {Binding CreatedAt}</span>
            </div>
            <em>{Binding Status}</em>
          </div>
        </cs:Repeater>
      </article>
    </section>

    <section class="dashboard quick-links">
      <cs:Repeater Items="{Binding Cards}">
        <div class="card" data-ui="clickable-card">
          <h3>{Binding Title}</h3>
          <p>{Binding Description}</p>
          <cs:Link NavigateTo="{Binding Url}">Learn more</cs:Link>
        </div>
      </cs:Repeater>
    </section>
  </div>
</cs:Content>

<cs:Content PlaceHolder="Scripts">
  <script src="/js/pages/home.js"></script>
</cs:Content>

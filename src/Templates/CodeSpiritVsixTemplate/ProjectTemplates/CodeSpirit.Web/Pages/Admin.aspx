<%@ Page ViewModel="$safeprojectname$.ViewModels.AdminViewModel" Title="Library Admin" %>

<cs:Content PlaceHolder="Head">
  <link rel="stylesheet" href="/css/pages/admin.css" />
</cs:Content>

<cs:Content PlaceHolder="Body">
  <div class="admin-page">
    <section class="admin-hero">
      <div>
        <p class="eyebrow">Backend Management</p>
        <h1>Library Admin</h1>
        <p>Manage collection growth, borrowing, and returns from one MVVM page.</p>
      </div>
      <a class="screen-link" href="/">Back to Dashboard</a>
    </section>

    <section class="admin-metrics">
      <cs:Repeater Items="{Binding Metrics}">
        <article class="admin-metric metric-{Binding Tone}">
          <span>{Binding Label}</span>
          <strong>{Binding Value}</strong>
          <small>{Binding Hint}</small>
        </article>
      </cs:Repeater>
    </section>

    <section class="admin-grid">
      <form class="admin-card" method="post" data-cs-vm>
        <h2>Add Book</h2>
        <label>Title<input name="NewTitle" value="{Binding NewTitle}" data-cs-bind="NewTitle" /></label>
        <label>Author<input name="NewAuthor" value="{Binding NewAuthor}" data-cs-bind="NewAuthor" /></label>
        <label>Category<input name="NewCategory" value="{Binding NewCategory}" data-cs-bind="NewCategory" /></label>
        <label>Location<input name="NewLocation" value="{Binding NewLocation}" data-cs-bind="NewLocation" /></label>
        <button type="submit" data-cs-command="AddBook">Add Book</button>
      </form>

      <form class="admin-card" method="post" data-cs-vm>
        <h2>Borrow or Return</h2>
        <label>Book Id<input name="BookId" value="{Binding BookId}" data-cs-bind="BookId" /></label>
        <label>Borrower<input name="Borrower" value="{Binding Borrower}" data-cs-bind="Borrower" /></label>
        <div class="button-row">
          <button type="submit" data-cs-command="BorrowBook">Borrow</button>
          <button type="submit" data-cs-command="ReturnBook">Return</button>
        </div>
      </form>

      <article class="admin-card activity-card">
        <h2>Activity</h2>
        <cs:Repeater Items="{Binding Activities}">
          <div class="activity activity-{Binding Tone}">
            <time>{Binding Time}</time>
            <span>{Binding Text}</span>
          </div>
        </cs:Repeater>
      </article>
    </section>

    <section class="book-table-card">
      <div class="panel-title">
        <h2>Collection</h2>
        <span>Use book id for borrow and return actions</span>
      </div>
      <table>
        <thead>
          <tr>
            <th>Id</th>
            <th>Title</th>
            <th>Author</th>
            <th>Category</th>
            <th>Status</th>
            <th>Borrower</th>
            <th>Location</th>
          </tr>
        </thead>
        <tbody>
          <cs:Repeater Items="{Binding Books}">
            <tr>
              <td>{Binding Id}</td>
              <td>{Binding Title}</td>
              <td>{Binding Author}</td>
              <td>{Binding Category}</td>
              <td><span class="status status-{Binding Status}">{Binding Status}</span></td>
              <td>{Binding Borrower}</td>
              <td>{Binding Location}</td>
            </tr>
          </cs:Repeater>
        </tbody>
      </table>
    </section>
  </div>
</cs:Content>

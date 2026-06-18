<%@ Page ViewModel="$safeprojectname$.ViewModels.HomeViewModel" Title="Home" %>

<!--
  CodeSpirit ASPX Page Template
  ViewModel: Controls page logic and data binding
  Layout: Master page for consistent UI
-->
<cs:Content PlaceHolder="Head">
  <link rel="stylesheet" href="/css/pages/home.css" />
</cs:Content>

<cs:Content PlaceHolder="Body">
  <div class="home-page">
    <section class="hero">
      <h1>{Binding WelcomeMessage}</h1>
      <p class="subtitle">{Binding Subtitle}</p>
    </section>

    <section class="dashboard">
      <cs:Repeater Items="{Binding Cards}">
        <div class="card" data-ui-clickable-card>
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

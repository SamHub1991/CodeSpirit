<%@ Page ViewModel="$safeprojectname$.Features.About.AboutViewModel" Title="About" %>

<cs:Content PlaceHolder="Body">
  <div class="about-page">
    <h1>{Binding AppName}</h1>
    <p class="version">Version {Binding Version}</p>
    <p>{Binding Description}</p>
  </div>
</cs:Content>

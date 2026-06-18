<%@ Page ViewModel="CodeSpirit.Web.ViewModels.WeatherViewModel" Title="Weather Forecast" %>

<cs:Content PlaceHolder="Body">
  <div class="weather-page">
    <h1>Weather Forecast</h1>

    <cs:Form>
      <div class="form-group">
        <label for="city">City</label>
        <input type="text" id="city" value="{Binding City}" cs:model="City" />
        <button type="submit" cs:command="{Binding Refresh}">Search</button>
      </div>
    </cs:Form>

    <cs:Conditional Visible="{Binding HasForecast}">
      <table class="weather-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Temp (C)</th>
            <th>Temp (F)</th>
            <th>Summary</th>
          </tr>
        </thead>
        <cs:Repeater Items="{Binding Forecast}">
          <tr>
            <td>{Binding Date:yyyy-MM-dd}</td>
            <td>{Binding TemperatureC}</td>
            <td>{Binding TemperatureF}</td>
            <td>{Binding Summary}</td>
          </tr>
        </cs:Repeater>
      </table>
    </cs:Conditional>
  </div>
</cs:Content>
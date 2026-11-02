# Menu visibility schedules

The settings screen is `/catalogs/menu-visibility`, accessible from the **Cấu hình hiển thị món** button on the menu-item catalog. Rules are stored in `MenuVisibilityRules` and applied by the sales menu APIs and the public QR menu API. QR order submission checks the schedule again to handle stale carts.

## Deploy

Apply the new EF migration to the intended database **before** starting the updated API. From `backend/AnFood.Persistence`:

```powershell
dotnet ef database update --context DataContext --project AnFood.Persistence.csproj --startup-project AnFood.Persistence.csproj
```

The migration has been generated, not applied to the configured database. Follow the normal deployment procedure for database permissions and backups.

## Rules

- Times use Vietnam time (UTC+7), independent of the server timezone.
- Weekly schedules use Sunday = 0 through Saturday = 6.
- Date ranges include both first and last date.
- No time ranges means the selected whole days.
- A time range includes its start and excludes its end.
- An overnight range belongs to the starting day (Monday 22:00–02:00 includes Tuesday 01:00).
- Multiple schedules/configurations combine with OR: a matching enabled rule hides its selected items only at its branch.
- Hidden items remain in existing orders; manual availability flags are unchanged.
- Open POS/QR menus refresh every 30 seconds while their tab is visible.

## Checks

From `backend`:

```powershell
dotnet run --project tests/MenuVisibilityChecks/MenuVisibilityChecks.csproj
```

From `frontend`:

```powershell
npm.cmd test -- --configuration=menu-visibility --watch=false --browsers=ChromeHeadless --progress=false
npm.cmd run build:local -- --progress=false
```

The isolated Karma configuration excludes unrelated global font imports; component templates and styles still compile.

Manual integration after migration: create a current-day rule for one branch, verify its selected dishes disappear in POS and QR, verify a second branch remains unaffected, toggle off to restore, and test a stale QR cart at the start of a hidden interval. Test edit/delete plus refreshing the settings page to confirm server persistence.

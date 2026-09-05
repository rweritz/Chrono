# Virtualized Angular grid for the calculation sheet

Research date: 2026-09-05

## Decision summary

Use **AG Grid Community** for the calculation sheet inside an Angular Material application.

It is the best V1 balance of two-dimensional rendering performance, server-windowed rows, dynamic series columns, keyboard interaction, custom status cells, and licensing:

- AG Grid virtualizes both rows and columns, inserting and removing cells as the user scrolls in either direction ([DOM virtualization](https://www.ag-grid.com/angular-data-grid/dom-virtualisation/)).
- The Community Infinite Row Model requests `startRow`/`endRow` blocks from an application datasource, supports bounded block caches, and does not require Enterprise ([Infinite Row Model](https://www.ag-grid.com/angular-data-grid/infinite-scrolling/)).
- Series columns can be added and removed by replacing `columnDefs`; matching columns retain state such as width and sort when they have stable IDs ([column definitions](https://www.ag-grid.com/angular-data-grid/column-definitions/)).
- It has built-in cell and header keyboard navigation, mouse/touch/keyboard column movement, and Angular cell components for state such as `Current`, `Recalculating`, `Stale`, and `Failed` ([keyboard interaction](https://www.ag-grid.com/angular-data-grid/keyboard-navigation/), [column moving](https://www.ag-grid.com/angular-data-grid/column-moving/), [cell components](https://www.ag-grid.com/angular-data-grid/component-cell-renderer/)).
- AG Grid Community is MIT-licensed; server-side row grouping and other advanced Server-Side Row Model features are an optional Enterprise upgrade, not a V1 requirement ([Community license](https://www.ag-grid.com/eula/AG-Grid-Community-License.html), [Community versus Enterprise](https://www.ag-grid.com/angular-data-grid/community-vs-enterprise/)).

Use Angular Material for the application shell, dialogs, menus, buttons, formula editor, version picker, and status messaging. Use Angular CDK Drag/Drop for dragging a named series from the library into the sheet. A drop changes the sheet definition and adds a dynamic AG Grid column; it does not copy or calculate data in the browser. AG Grid is the view, while formula parsing, evaluation, version selection, and recalculation remain .NET backend responsibilities.

## Required fit

Prototype B needs more than a normal data table. The central surface is a wide, spreadsheet-like projection in which:

- each row represents a timestamp in the selected server-side window;
- the timestamp column stays pinned while imported and derived time series become dynamic columns;
- a user can drag a series from the library into a precise sheet position and reorder existing columns;
- cells show numeric values or missing values, while headers and supporting cells show formula/version/recalculation state;
- the browser renders only a small row block and visible columns rather than loading a full time series; and
- keyboard and assistive-technology users have non-drag alternatives.

The grid must not become the calculation authority. A calculated column displays a persisted time-series version produced by the backend. The formula editor may preview and validate, but saving a formula creates a definition version and schedules backend recalculation.

## Options compared

| Criterion | AG Grid Community | Handsontable | Kendo UI Grid | Angular Material/CDK table |
|---|---|---|---|---|
| Vertical virtualization | Built in; rendered row buffer is configurable ([docs](https://www.ag-grid.com/angular-data-grid/dom-virtualisation/)) | Built in and enabled by default ([docs](https://handsontable.com/docs/17.0/angular-data-grid/row-virtualization/)) | Built in with fixed row height and `skip`/`pageSize` ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/scroll-modes/virtual)) | Material table can be wrapped in CDK virtual scroll for visible rows ([upstream guide](https://github.com/angular/components/blob/main/src/material/table/table.md#virtual-scrolling)) |
| Horizontal virtualization | Built in ([docs](https://www.ag-grid.com/angular-data-grid/dom-virtualisation/)) | Built in by default through DOM virtualization ([options](https://handsontable.com/docs/angular-data-grid/api/options/)) | Explicit `virtualColumns`; every column needs a width ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/columns/virtual)) | CDK viewport has one orientation, vertical **or** horizontal; a production two-axis sheet would be custom work ([CDK source](https://github.com/angular/components/blob/main/src/cdk/scrolling/virtual-scroll-viewport.ts)) |
| Dynamic columns | Replace `columnDefs`; stable `colId` values let the grid preserve matching state ([docs](https://www.ag-grid.com/angular-data-grid/column-definitions/)) | Column metadata is configurable and columns can be moved, but server-backed mode conflicts with manual column movement ([server-side data](https://handsontable.com/docs/17.1/angular-data-grid/server-side-data/)) | Columns can be generated from a configuration array with Angular `@for` ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/columns/define-columns)) | Displayed column arrays can dynamically reorder/include/exclude definitions ([upstream guide](https://github.com/angular/components/blob/main/src/material/table/table.md)) |
| Keyboard and accessibility | Rich grid/header keyboard navigation and ARIA roles; virtualization has documented screen-reader limitations ([keyboard](https://www.ag-grid.com/angular-data-grid/keyboard-navigation/), [accessibility](https://www.ag-grid.com/angular-data-grid/accessibility/)) | Spreadsheet keyboard model and WCAG guidance; vendor recommends disabling virtualization for the most reliable accessibility tree ([accessibility](https://handsontable.com/docs/17.0/angular-data-grid/accessibility/)) | Keyboard navigation is enabled by default in current releases; vendor documents WCAG 2.2 AA/Section 508 conformance subject to configuration and testing ([keyboard](https://www.telerik.com/kendo-angular-ui/components/grid/keyboard-navigation), [accessibility](https://www.telerik.com/kendo-angular-ui/components/grid/accessibility)) | Changing `MatTable` to `role="grid"` changes roles but adds no keyboard handling or focus management ([upstream guide](https://github.com/angular/components/blob/main/src/material/table/table.md#accessibility)) |
| Drag/drop composition | Header reordering is built in; use CDK Drag/Drop for external library-to-sheet composition ([column moving](https://www.ag-grid.com/angular-data-grid/column-moving/), [CDK Drag/Drop](https://angular.dev/guide/drag-drop)) | Excellent in-grid column drag, but `manualColumnMove` disables the new server DataProvider ([server-side data](https://handsontable.com/docs/17.1/angular-data-grid/server-side-data/)) | Header drag reordering is built in ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/columns/reordering)); external composition still needs application integration | CDK supports free dragging, list reorder, cross-list transfer, handles, previews, and placeholders; the application updates the model ([docs](https://angular.dev/guide/drag-drop)) |
| Custom/status cells | Angular cell and loading renderer components; renderers exist only while their virtualized cells are visible ([docs](https://www.ag-grid.com/angular-data-grid/component-cell-renderer/)) | Custom cell renderers, including Angular components in the current wrapper ([docs](https://handsontable.com/docs/17.1/angular-data-grid/cell-renderer/)) | Angular cell/header/footer templates expose row, column, and data context ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/columns/templates)) | Arbitrary Angular templates are supported, so all behavior is possible but must be built ([upstream guide](https://github.com/angular/components/blob/main/src/material/table/table.md)) |
| Server-side windowing | Community Infinite Row Model fetches blocks; Enterprise SSRM adds advanced operations ([Infinite](https://www.ag-grid.com/angular-data-grid/infinite-scrolling/), [SSRM](https://www.ag-grid.com/angular-data-grid/server-side-model-datasource/)) | Version 17.1 DataProvider fetches page/page-size results, but requires all CRUD callbacks and conflicts with manual column movement and multi-column sorting ([docs](https://handsontable.com/docs/17.1/angular-data-grid/server-side-data/)) | Remote virtual scrolling loads `GridDataResult` windows on `pageChange`; callers should debounce remote requests ([docs](https://www.telerik.com/kendo-angular-ui/components/grid/scroll-modes/virtual)) | A custom `DataSource` can encapsulate retrieval, but window/cache/loading behavior is application code ([upstream guide](https://github.com/angular/components/blob/main/src/material/table/table.md)) |
| License | Community is MIT; Enterprise is commercial ([license](https://www.ag-grid.com/eula/AG-Grid-Community-License.html), [pricing/features](https://www.ag-grid.com/license-pricing/)) | Commercial production license; the free key is restricted to non-commercial/evaluation use ([license key](https://handsontable.com/docs/angular-data-grid/license-key/)) | Every Angular component requires a trial or commercial license ([licensing](https://www.telerik.com/kendo-angular-ui/components/licensing)) | MIT ([repository license](https://github.com/angular/components/blob/main/LICENSE)) |
| Current Angular fit | AG Grid 36 supports Angular 20–22 and zoneless Angular ([compatibility](https://www.ag-grid.com/angular-data-grid/compatibility/)) | Current wrapper supports Angular 16+; Angular 21+ requires wrapper 16.2+ ([installation](https://handsontable.com/docs/angular-data-grid/installation/)) | Current releases support active/LTS Angular 20–22, but Kendo does not yet support zoneless change detection ([requirements](https://www.telerik.com/kendo-angular-ui/components/installation/requirements), [FAQ](https://www.telerik.com/kendo-angular-ui/components/faq)) | Released with Angular and therefore avoids a third-party compatibility matrix |
| Overall | **Recommended** | Best spreadsheet feel, but weaker server/reordering/licensing fit | Strong paid alternative | Best for the surrounding Material UI, not the central sheet |

## Candidate detail

### AG Grid Community — recommended

AG Grid's rendering model directly matches a wide sheet. It creates DOM only for the visible rows and columns and removes them when they leave the viewport. Row buffering is configurable, while columns intentionally have no off-screen buffer ([DOM virtualization](https://www.ag-grid.com/angular-data-grid/dom-virtualisation/)). The pinned timestamp column is outside the central horizontal viewport, so it remains visible while series columns virtualize ([column pinning](https://www.ag-grid.com/angular-data-grid/column-pinning/)).

The Community Infinite Row Model is sufficient for V1. Its datasource receives `startRow`, exclusive `endRow`, sort state, and filter state. `cacheBlockSize`, `maxBlocksInCache`, and `maxConcurrentDatasourceRequests` bound the amount of browser data and backend concurrency ([Infinite Row Model](https://www.ag-grid.com/angular-data-grid/infinite-scrolling/)). The Enterprise Server-Side Row Model also fetches windows, adds richer grouping/pivoting/transaction behavior, and can refresh server-side stores, but those features are not needed for a timestamp-by-series sheet ([SSRM datasource](https://www.ag-grid.com/angular-data-grid/server-side-model-datasource/), [Community versus Enterprise](https://www.ag-grid.com/angular-data-grid/community-vs-enterprise/)). Starting with Community avoids making a commercial license part of the architecture.

Dynamic column definitions are a strong fit for a saved calculation sheet. Give the timestamp column a fixed ID and each series column a stable ID derived from the sheet-column identity—not the mutable display name. When the set of definitions changes, AG Grid distinguishes retained, new, and removed columns and preserves state on retained ones ([updating column definitions](https://www.ag-grid.com/javascript-data-grid/column-updating-definitions/)). Persist the order, width, pinned state, and selected version in the sheet model; do not treat incidental grid state as the authoritative sheet definition.

For interaction, AG Grid already supports arrow/Page/Home/End navigation, editing keys, focusable headers, keyboard header movement with Shift+Arrow, and keyboard header resizing with Alt+Arrow ([keyboard interaction](https://www.ag-grid.com/angular-data-grid/keyboard-navigation/)). Mouse/touch header movement is also built in ([column moving](https://www.ag-grid.com/angular-data-grid/column-moving/)). External library-to-sheet dragging should use Angular CDK Drag/Drop because it provides handles, previews, placeholders, and cross-list transfer primitives without coupling the series library to grid internals ([CDK Drag/Drop](https://angular.dev/guide/drag-drop)). The drop handler inserts a sheet column through the application state/API and reapplies `columnDefs`.

Formula and status presentation are feasible without client-side calculation. An Angular cell renderer can show a numeric value, missing-value marker, stale indicator, failure affordance, or loading skeleton ([cell components](https://www.ag-grid.com/angular-data-grid/component-cell-renderer/)). A custom header can show the series name, version mode (`Latest` or pinned), formula badge, and recalculation status, while leaving column movement and resizing to the grid ([custom headers](https://www.ag-grid.com/angular-data-grid/column-headers-components/)). Keep renderers lightweight because virtual scrolling repeatedly creates and destroys them.

AG Grid is not an Angular Material component, but it offers a Material theme explicitly intended for applications using Material Design, plus typed theming parameters and Material icon parts ([built-in themes](https://www.ag-grid.com/angular-data-grid/themes/), [styling API](https://www.ag-grid.com/angular-data-grid/styling-tutorial/)). The built-in Material preset follows Material 2, so a new Material 3 application should probably start from Quartz and align its colors, density, typography, focus ring, and icons with the application's Angular Material theme rather than expecting exact token sharing.

Do **not** use AG Grid's client expression or calculated-column features as Chrono's formula engine. Grid expressions belong to a column and run in the browser ([grid expressions](https://www.ag-grid.com/angular-data-grid/cell-expressions/)); they cannot provide the authoritative formula version, exact dependency-version provenance, asynchronous recalculation, or persisted output versions required by this application.

### Handsontable — strongest spreadsheet feel, weaker architecture fit

Handsontable looks and behaves most like a traditional spreadsheet. It virtualizes rows and columns by default, supports keyboard navigation, manual column movement, and custom cell renderers ([virtualization options](https://handsontable.com/docs/angular-data-grid/api/options/), [column moving](https://handsontable.com/docs/17.0/angular-data-grid/column-moving/), [cell renderer](https://handsontable.com/docs/17.1/angular-data-grid/cell-renderer/)). Its current Angular wrapper supports Angular 16 and newer, with wrapper 16.2 or later required for Angular 21+ ([installation](https://handsontable.com/docs/angular-data-grid/installation/)).

The decisive drawback is the interaction between server data and column composition. Handsontable 17.1's DataProvider fetches server pages and forwards single-column sorting/filtering, but requires `rowId`, `fetchRows`, and create/update/remove callbacks even for a read-mostly surface. More importantly, enabling `manualColumnMove`, `manualRowMove`, `multiColumnSorting`, or `trimRows` prevents the DataProvider from enabling ([server-side data](https://handsontable.com/docs/17.1/angular-data-grid/server-side-data/)). The feature that best supports future scale therefore conflicts with the sheet's user-arranged columns.

Accessibility also has a scale tradeoff: Handsontable documents keyboard and screen-reader support but recommends disabling row and column DOM virtualization to create the most reliable accessibility tree, and lists known screen-reader limitations for virtual/frozen grids ([accessibility](https://handsontable.com/docs/17.0/angular-data-grid/accessibility/)). Finally, production commercial use requires a paid license; the free key is for non-commercial and evaluation purposes ([license key](https://handsontable.com/docs/angular-data-grid/license-key/)). Reconsider Handsontable only if Excel-like direct editing, range selection, and copy/paste become more important than server windowing, and the team accepts both its commercial license and the DataProvider constraint.

### Kendo UI Grid — strong paid alternative

Kendo covers the mechanics well: virtual rows, separately enabled virtual columns, remote `GridDataResult` binding, dynamic columns generated from configuration, draggable header reordering, customizable templates, and default keyboard navigation ([row virtualization](https://www.telerik.com/kendo-angular-ui/components/grid/scroll-modes/virtual), [column virtualization](https://www.telerik.com/kendo-angular-ui/components/grid/columns/virtual), [remote binding](https://www.telerik.com/kendo-angular-ui/components/grid/data-binding/remote-data), [dynamic columns](https://www.telerik.com/kendo-angular-ui/components/grid/columns/define-columns), [reordering](https://www.telerik.com/kendo-angular-ui/components/grid/columns/reordering), [keyboard navigation](https://www.telerik.com/kendo-angular-ui/components/grid/keyboard-navigation)). Its cell templates can comfortably express status badges and value states ([column templates](https://www.telerik.com/kendo-angular-ui/components/grid/columns/templates)).

The remote virtual-scroll contract is suitable for a row window: the application handles `pageChange`, supplies `skip`, `pageSize`, data, and total count, and should debounce frequent remote page-change events. It requires equal predefined row heights, and Telerik notes that browser maximum element-height limits can eventually affect very large virtual scroll ranges ([virtual scrolling](https://www.telerik.com/kendo-angular-ui/components/grid/scroll-modes/virtual)).

Kendo has the strongest first-party accessibility claim of the candidates: its grid documentation states WCAG 2.2 AA and Section 508 conformance, with a warning that feature/configuration combinations and customizations still require testing ([accessibility](https://www.telerik.com/kendo-angular-ui/components/grid/accessibility)). It supports the current active/LTS Angular releases, but its FAQ says zoneless change detection is not yet supported ([requirements](https://www.telerik.com/kendo-angular-ui/components/installation/requirements), [FAQ](https://www.telerik.com/kendo-angular-ui/components/faq)). All Angular components require a trial or commercial license ([licensing](https://www.telerik.com/kendo-angular-ui/components/licensing)). It is a credible option if the organization already owns Telerik licenses or values vendor support enough to pay for it; otherwise AG Grid Community supplies the V1 capabilities with less lock-in.

### Angular Material/CDK table — use around the sheet, not as the sheet

`MatTable` is deliberately a rendering foundation rather than a spreadsheet component. It supports arbitrary cell templates, dynamic displayed-column lists, custom `DataSource` implementations, sticky columns, and integration with CDK Drag/Drop ([upstream table guide](https://github.com/angular/components/blob/main/src/material/table/table.md)). The Angular components repository is MIT-licensed ([license](https://github.com/angular/components/blob/main/LICENSE)).

Current upstream code supports wrapping a Material table in `cdk-virtual-scroll-viewport`, but the documented table integration virtualizes visible **rows** and has limitations such as forced fixed layout and no conditional row templates ([virtual-scrolling section](https://github.com/angular/components/blob/main/src/material/table/table.md#virtual-scrolling)). The CDK viewport itself has a single `orientation` input whose value is `vertical` or `horizontal`, not both ([viewport source](https://github.com/angular/components/blob/main/src/cdk/scrolling/virtual-scroll-viewport.ts)). Achieving synchronized, two-axis virtualization with sticky timestamp/header cells would therefore be bespoke grid engineering.

Likewise, applying `role="grid"` changes table and cell roles but does not add focus management or keyboard input behavior ([accessibility section](https://github.com/angular/components/blob/main/src/material/table/table.md#accessibility)). All spreadsheet navigation, selection, loading/caching, column resizing, visible-range tracking, and assistive announcements would be application code. Material/CDK remains the right choice for the application shell and external drag interactions, but building the central sheet from it would spend V1 effort recreating a mature data grid.

## Recommended frontend/backend contract

For V1, use the Community Infinite Row Model with a purpose-built read endpoint. A request should include:

- calculation-sheet identity/version;
- selected time-range and output period/alignment;
- `startRow` and `endRow` from the AG Grid datasource;
- the ordered sheet-column identities and selected time-series version mode (`Latest` or a pinned version); and
- an opaque window revision/token so a recalculation or dependency update cannot silently mix versions across cached blocks.

The response should contain:

- stable row IDs based on the aligned timestamp;
- timestamp plus a value/missing-state entry for every requested sheet column;
- exact resolved time-series version IDs used for the window;
- total/last row information for the requested range; and
- sheet/series recalculation status and an invalidation revision.

For roughly 100 V1 columns, return all configured column values in each row block while AG Grid virtualizes only the DOM. This keeps the datasource simple and still prevents full-series loading. AG Grid fires `virtualColumnsChanged` when the rendered horizontal column set changes ([grid events](https://www.ag-grid.com/angular-data-grid/grid-events/)); a future deployment with thousands of columns can add a column-window parameter and refetch without changing the domain API that identifies sheet columns. That future optimization should be measured first because horizontal request churn can be worse than transferring moderately wide row blocks.

When the selected range, period, sheet definition, pinned versions, or backend revision changes, replace/reset the datasource so the Infinite Row Model clears its block cache; AG Grid documents datasource replacement as the reset mechanism ([Infinite Row Model](https://www.ag-grid.com/angular-data-grid/infinite-scrolling/)). Cancel superseded HTTP requests and ignore responses whose revision no longer matches the active sheet.

## Accessibility and interaction requirements

No virtualized grid can claim that its high-density mode is equivalent to a complete semantic table: off-screen cells are absent from the DOM. AG Grid explicitly documents the conflict and recommends ordered DOM, pagination, and disabled row/column virtualization when a screen reader needs every element rendered. It also notes that SSRM cannot always announce row count ([accessibility guidance](https://www.ag-grid.com/angular-data-grid/accessibility/)).

V1 should therefore provide two deliberate modes:

1. **Working sheet:** two-axis virtualization, pinned timestamp, keyboard grid navigation, and fast server block loading.
2. **Accessible paged view:** bounded page size, deterministic DOM order (`ensureDomOrder`), virtualization disabled for the page, plain text equivalents for status/color, and a clear accessible name/description.

Every drag action needs a non-drag equivalent. At minimum, the series library needs an **Add to sheet** button/menu with insertion-position selection, and each column needs keyboard-accessible Move left, Move right, and Remove commands. Status must never rely on color alone; announce recalculation completion/failure through an Angular Material live region/snackbar and keep detailed status available in the column header/menu.

## Proof-of-fit spike

Before making the grid choice an ADR, build a narrow Angular spike using only AG Grid Community plus Angular Material/CDK. It should prove:

1. **Dynamic sheet composition:** drag a series from the Material library into an exact column position; add/remove/reorder columns without losing horizontal scroll, keyboard focus, widths, pinned timestamp, or loaded row blocks. Prove the same workflow without drag.
2. **Backend windowing:** serve a sparse timestamp union from a small ASP.NET Core endpoint through `startRow`/`endRow`; cap `cacheBlockSize`, `maxBlocksInCache`, and concurrent requests; cancel superseded requests; refresh cleanly on a version revision.
3. **Version/status UI:** render `Latest` and pinned version headers plus `Current`, `Recalculating`, `Stale`, and `Failed` states with accessible labels and loading skeletons.
4. **Scale:** measure scroll/frame behavior and payload size at 100 columns and enough synthetic rows that only server blocks can be practical. Confirm that custom renderers do not retain subscriptions when virtualized away.
5. **Accessibility:** test keyboard-only composition and navigation, axe checks, and at least one screen reader in both working-sheet and accessible-paged modes.
6. **Visual integration:** start from Quartz or Material theme, align density/colors/typography/focus with Angular Material, and verify light/dark/high-contrast presentations.

Acceptance should be based on those observed interactions, not on vendor demos alone. If AG Grid Community fails the spike, evaluate Kendo next when commercial licensing is acceptable. Evaluate Handsontable only when spreadsheet-native editing becomes a stronger requirement than rearrangeable server-backed columns.

## Consequences for the V1 plan

- Add `ag-grid-angular`/`ag-grid-community`; do not add `ag-grid-enterprise` unless a separately approved feature needs it. The two AG Grid packages are installed together through `ag-grid-angular` ([installation](https://www.ag-grid.com/angular-data-grid/installation/)).
- Keep Angular Material as the design system and Angular CDK as the external drag/drop and accessibility primitive layer.
- Treat the grid datasource as an adapter over a Chrono window-query API. Do not leak AG Grid request types into the application/domain layer.
- Persist sheet column identity/order/version-selection separately from the formula definitions and time-series versions.
- Keep formula text and evaluation out of grid expressions; the backend remains authoritative.
- Plan an accessible paged presentation from the first slice instead of attempting to retrofit full screen-reader semantics onto a huge virtual surface.
- Record the exact Angular and AG Grid versions after scaffolding. As of this research, AG Grid 36 supports Angular 20–22 and requires TypeScript 5.8.3 or newer ([compatibility](https://www.ag-grid.com/angular-data-grid/compatibility/)).

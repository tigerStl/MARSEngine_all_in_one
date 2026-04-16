## MARS Web Demo UIs and VS Code Webview

This folder contains four small web demo projects and a VS Code-style webview panel.

- **`vue-summit-swap`**: Vue 3 single-page app simulating Finastra Summit Swap Trade entry and simple cash flow calculation.
- **`react-summit-swap`**: React single-page app with the same swap trade and cash flow UI/logic as the Vue app.
- **`angular-loaniq`**: Angular-style (AngularJS 1.x) web UI that roughly mimics a LoanIQ-style loan trade entry screen.
- **`jquery-bond`**: jQuery-based web UI that roughly mimics a bond trading ticket.
- **`vscode-extension-web`**: VS Code extension skeleton that shows a webview panel with the same layout as the existing `MarsPlugins/VSCode` extension (object tree, toolbar, object info, test steps).

### Install and run the web demos

From this folder:

```bash
npm install

# Vue Finastra Summit Swap UI
npm run serve:vue-swap

# React Finastra Summit Swap UI
npm run serve:react-swap

# LoanIQ-style AngularJS UI
npm run serve:angular-loaniq

# Bond trading jQuery UI
npm run serve:jquery-bond
```

Then open the printed local URL in your browser.


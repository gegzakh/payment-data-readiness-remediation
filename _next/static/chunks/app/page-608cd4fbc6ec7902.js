(self.webpackChunk_N_E = self.webpackChunk_N_E || []).push([
  [974],
  {
    2560: (e, t, s) => {
      Promise.resolve().then(s.bind(s, 5413));
    },
    5413: (e, t, s) => {
      "use strict";
      (s.r(t), s.d(t, { default: () => ez }));
      var r = s(2204),
        a = s(3360),
        n = s(6093),
        i = s(863),
        d = s(8207),
        l = s(5955),
        o = s(8078),
        c = s(2553),
        m = s(2151),
        x = s(5723),
        u = s(6538),
        p = s(7005),
        h = s(1113),
        f = s(8622),
        b = s(4554),
        g = s(2592),
        j = s(1808),
        v = s(2244),
        N = s(4241),
        y = s(2744),
        w = s(2407),
        C = s(8486),
        k = s(1610),
        A = s(8689),
        S = s(333),
        z = s(2241),
        T = s(3024),
        P = s(3927),
        R = s(1333),
        _ = s(9286),
        L = s(5261),
        D = s(5241);
      function O(...e) {
        return (0, D.QP)((0, L.$)(e));
      }
      let E = (0, R.F)(
        "inline-flex w-fit shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border border-transparent px-2 py-0.5 text-xs font-medium whitespace-nowrap transition-[color,box-shadow] focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 [&>svg]:pointer-events-none [&>svg]:size-3",
        {
          variants: {
            variant: {
              default:
                "bg-primary text-primary-foreground [a&]:hover:bg-primary/90",
              secondary:
                "bg-secondary text-secondary-foreground [a&]:hover:bg-secondary/90",
              destructive:
                "bg-destructive text-white focus-visible:ring-destructive/20 dark:bg-destructive/60 dark:focus-visible:ring-destructive/40 [a&]:hover:bg-destructive/90",
              outline:
                "border-border text-foreground [a&]:hover:bg-accent [a&]:hover:text-accent-foreground",
              ghost: "[a&]:hover:bg-accent [a&]:hover:text-accent-foreground",
              link: "text-primary underline-offset-4 [a&]:hover:underline",
            },
          },
          defaultVariants: { variant: "default" },
        },
      );
      function I({
        className: e,
        variant: t = "default",
        asChild: s = !1,
        ...a
      }) {
        let n = s ? _.bL : "span";
        return (0, r.jsx)(n, {
          "data-slot": "badge",
          "data-variant": t,
          className: O(E({ variant: t }), e),
          ...a,
        });
      }
      let F = (0, R.F)(
        "inline-flex shrink-0 items-center justify-center gap-2 rounded-md text-sm font-medium whitespace-nowrap transition-all outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
        {
          variants: {
            variant: {
              default: "bg-primary text-primary-foreground hover:bg-primary/90",
              destructive:
                "bg-destructive text-white hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:bg-destructive/60 dark:focus-visible:ring-destructive/40",
              outline:
                "border bg-background shadow-xs hover:bg-accent hover:text-accent-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
              secondary:
                "bg-secondary text-secondary-foreground hover:bg-secondary/80",
              ghost:
                "hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50",
              link: "text-primary underline-offset-4 hover:underline",
            },
            size: {
              default: "h-9 px-4 py-2 has-[>svg]:px-3",
              xs: "h-6 gap-1 rounded-md px-2 text-xs has-[>svg]:px-1.5 [&_svg:not([class*='size-'])]:size-3",
              sm: "h-8 gap-1.5 rounded-md px-3 has-[>svg]:px-2.5",
              lg: "h-10 rounded-md px-6 has-[>svg]:px-4",
              icon: "size-9",
              "icon-xs":
                "size-6 rounded-md [&_svg:not([class*='size-'])]:size-3",
              "icon-sm": "size-8",
              "icon-lg": "size-10",
            },
          },
          defaultVariants: { variant: "default", size: "default" },
        },
      );
      function U({
        className: e,
        variant: t = "default",
        size: s = "default",
        asChild: a = !1,
        ...n
      }) {
        let i = a ? _.bL : "button";
        return (0, r.jsx)(i, {
          "data-slot": "button",
          "data-variant": t,
          "data-size": s,
          className: O(F({ variant: t, size: s, className: e })),
          ...n,
        });
      }
      function B({ className: e, type: t, ...s }) {
        return (0, r.jsx)("input", {
          type: t,
          "data-slot": "input",
          className: O(
            "h-9 w-full min-w-0 rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none selection:bg-primary selection:text-primary-foreground file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
            "focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50",
            "aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40",
            e,
          ),
          ...s,
        });
      }
      var M = s(8708),
        V = s(5587),
        $ = s(2236);
      function H({ ...e }) {
        return (0, r.jsx)($.bL, { "data-slot": "select", ...e });
      }
      function q({ ...e }) {
        return (0, r.jsx)($.WT, { "data-slot": "select-value", ...e });
      }
      function J({ className: e, size: t = "default", children: s, ...a }) {
        return (0, r.jsxs)($.l9, {
          "data-slot": "select-trigger",
          "data-size": t,
          className: O(
            "flex w-fit items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 py-2 text-sm whitespace-nowrap shadow-xs transition-[color,box-shadow] outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 data-[placeholder]:text-muted-foreground data-[size=default]:h-9 data-[size=sm]:h-8 *:data-[slot=select-value]:line-clamp-1 *:data-[slot=select-value]:flex *:data-[slot=select-value]:items-center *:data-[slot=select-value]:gap-2 dark:bg-input/30 dark:hover:bg-input/50 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4 [&_svg:not([class*='text-'])]:text-muted-foreground",
            e,
          ),
          ...a,
          children: [
            s,
            (0, r.jsx)($.In, {
              asChild: !0,
              children: (0, r.jsx)(M.A, { className: "size-4 opacity-50" }),
            }),
          ],
        });
      }
      function K({
        className: e,
        children: t,
        position: s = "item-aligned",
        align: a = "center",
        ...n
      }) {
        return (0, r.jsx)($.ZL, {
          children: (0, r.jsxs)($.UC, {
            "data-slot": "select-content",
            className: O(
              "relative z-50 max-h-(--radix-select-content-available-height) min-w-[8rem] origin-(--radix-select-content-transform-origin) overflow-x-hidden overflow-y-auto rounded-md border bg-popover text-popover-foreground shadow-md data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95 data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95",
              "popper" === s &&
                "data-[side=bottom]:translate-y-1 data-[side=left]:-translate-x-1 data-[side=right]:translate-x-1 data-[side=top]:-translate-y-1",
              e,
            ),
            position: s,
            align: a,
            ...n,
            children: [
              (0, r.jsx)(Y, {}),
              (0, r.jsx)($.LM, {
                className: O(
                  "p-1",
                  "popper" === s &&
                    "h-[var(--radix-select-trigger-height)] w-full min-w-[var(--radix-select-trigger-width)] scroll-my-1",
                ),
                children: t,
              }),
              (0, r.jsx)(W, {}),
            ],
          }),
        });
      }
      function G({ className: e, children: t, ...s }) {
        return (0, r.jsxs)($.q7, {
          "data-slot": "select-item",
          className: O(
            "relative flex w-full cursor-default items-center gap-2 rounded-sm py-1.5 pr-8 pl-2 text-sm outline-hidden select-none focus:bg-accent focus:text-accent-foreground data-[disabled]:pointer-events-none data-[disabled]:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4 [&_svg:not([class*='text-'])]:text-muted-foreground *:[span]:last:flex *:[span]:last:items-center *:[span]:last:gap-2",
            e,
          ),
          ...s,
          children: [
            (0, r.jsx)("span", {
              "data-slot": "select-item-indicator",
              className:
                "absolute right-2 flex size-3.5 items-center justify-center",
              children: (0, r.jsx)($.VF, {
                children: (0, r.jsx)(T.A, { className: "size-4" }),
              }),
            }),
            (0, r.jsx)($.p4, { children: t }),
          ],
        });
      }
      function Y({ className: e, ...t }) {
        return (0, r.jsx)($.PP, {
          "data-slot": "select-scroll-up-button",
          className: O(
            "flex cursor-default items-center justify-center py-1",
            e,
          ),
          ...t,
          children: (0, r.jsx)(V.A, { className: "size-4" }),
        });
      }
      function W({ className: e, ...t }) {
        return (0, r.jsx)($.wn, {
          "data-slot": "select-scroll-down-button",
          className: O(
            "flex cursor-default items-center justify-center py-1",
            e,
          ),
          ...t,
          children: (0, r.jsx)(M.A, { className: "size-4" }),
        });
      }
      var X = s(4220),
        Z = s(8312);
      function Q({ ...e }) {
        return (0, r.jsx)(Z.bL, { "data-slot": "sheet", ...e });
      }
      function ee({ ...e }) {
        return (0, r.jsx)(Z.ZL, { "data-slot": "sheet-portal", ...e });
      }
      function et({ className: e, ...t }) {
        return (0, r.jsx)(Z.hJ, {
          "data-slot": "sheet-overlay",
          className: O(
            "fixed inset-0 z-50 bg-black/50 data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:animate-in data-[state=open]:fade-in-0",
            e,
          ),
          ...t,
        });
      }
      function es({
        className: e,
        children: t,
        side: s = "right",
        showCloseButton: a = !0,
        ...n
      }) {
        return (0, r.jsxs)(ee, {
          children: [
            (0, r.jsx)(et, {}),
            (0, r.jsxs)(Z.UC, {
              "data-slot": "sheet-content",
              className: O(
                "fixed z-50 flex flex-col gap-4 bg-background shadow-lg transition ease-in-out data-[state=closed]:animate-out data-[state=closed]:duration-300 data-[state=open]:animate-in data-[state=open]:duration-500",
                "right" === s &&
                  "inset-y-0 right-0 h-full w-3/4 border-l data-[state=closed]:slide-out-to-right data-[state=open]:slide-in-from-right sm:max-w-sm",
                "left" === s &&
                  "inset-y-0 left-0 h-full w-3/4 border-r data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left sm:max-w-sm",
                "top" === s &&
                  "inset-x-0 top-0 h-auto border-b data-[state=closed]:slide-out-to-top data-[state=open]:slide-in-from-top",
                "bottom" === s &&
                  "inset-x-0 bottom-0 h-auto border-t data-[state=closed]:slide-out-to-bottom data-[state=open]:slide-in-from-bottom",
                e,
              ),
              ...n,
              children: [
                t,
                a &&
                  (0, r.jsxs)(Z.bm, {
                    className:
                      "absolute top-4 right-4 rounded-xs opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:ring-2 focus:ring-ring focus:ring-offset-2 focus:outline-hidden disabled:pointer-events-none data-[state=open]:bg-secondary",
                    children: [
                      (0, r.jsx)(X.A, { className: "size-4" }),
                      (0, r.jsx)("span", {
                        className: "sr-only",
                        children: "Close",
                      }),
                    ],
                  }),
              ],
            }),
          ],
        });
      }
      function er({ className: e, ...t }) {
        return (0, r.jsx)("div", {
          "data-slot": "sheet-header",
          className: O("flex flex-col gap-1.5 p-4", e),
          ...t,
        });
      }
      function ea({ className: e, ...t }) {
        return (0, r.jsx)("div", {
          "data-slot": "sheet-footer",
          className: O("mt-auto flex flex-col gap-2 p-4", e),
          ...t,
        });
      }
      function en({ className: e, ...t }) {
        return (0, r.jsx)(Z.hE, {
          "data-slot": "sheet-title",
          className: O("font-semibold text-foreground", e),
          ...t,
        });
      }
      function ei({ className: e, ...t }) {
        return (0, r.jsx)(Z.VY, {
          "data-slot": "sheet-description",
          className: O("text-sm text-muted-foreground", e),
          ...t,
        });
      }
      function ed({ className: e, ...t }) {
        return (0, r.jsx)("div", {
          "data-slot": "table-container",
          className: "relative w-full overflow-x-auto",
          children: (0, r.jsx)("table", {
            "data-slot": "table",
            className: O("w-full caption-bottom text-sm", e),
            ...t,
          }),
        });
      }
      function el({ className: e, ...t }) {
        return (0, r.jsx)("thead", {
          "data-slot": "table-header",
          className: O("[&_tr]:border-b", e),
          ...t,
        });
      }
      function eo({ className: e, ...t }) {
        return (0, r.jsx)("tbody", {
          "data-slot": "table-body",
          className: O("[&_tr:last-child]:border-0", e),
          ...t,
        });
      }
      function ec({ className: e, ...t }) {
        return (0, r.jsx)("tr", {
          "data-slot": "table-row",
          className: O(
            "border-b transition-colors hover:bg-muted/50 has-aria-expanded:bg-muted/50 data-[state=selected]:bg-muted",
            e,
          ),
          ...t,
        });
      }
      function em({ className: e, ...t }) {
        return (0, r.jsx)("th", {
          "data-slot": "table-head",
          className: O(
            "h-10 px-2 text-left align-middle font-medium whitespace-nowrap text-foreground [&:has([role=checkbox])]:pr-0 [&>[role=checkbox]]:translate-y-[2px]",
            e,
          ),
          ...t,
        });
      }
      function ex({ className: e, ...t }) {
        return (0, r.jsx)("td", {
          "data-slot": "table-cell",
          className: O(
            "p-2 align-middle whitespace-nowrap [&:has([role=checkbox])]:pr-0 [&>[role=checkbox]]:translate-y-[2px]",
            e,
          ),
          ...t,
        });
      }
      var eu = s(9672);
      function ep({ className: e, orientation: t = "horizontal", ...s }) {
        return (0, r.jsx)(eu.bL, {
          "data-slot": "tabs",
          "data-orientation": t,
          orientation: t,
          className: O(
            "group/tabs flex gap-2 data-[orientation=horizontal]:flex-col",
            e,
          ),
          ...s,
        });
      }
      let eh = (0, R.F)(
        "group/tabs-list inline-flex w-fit items-center justify-center rounded-lg p-[3px] text-muted-foreground group-data-[orientation=horizontal]/tabs:h-9 group-data-[orientation=vertical]/tabs:h-fit group-data-[orientation=vertical]/tabs:flex-col data-[variant=line]:rounded-none",
        {
          variants: {
            variant: { default: "bg-muted", line: "gap-1 bg-transparent" },
          },
          defaultVariants: { variant: "default" },
        },
      );
      function ef({ className: e, variant: t = "default", ...s }) {
        return (0, r.jsx)(eu.B8, {
          "data-slot": "tabs-list",
          "data-variant": t,
          className: O(eh({ variant: t }), e),
          ...s,
        });
      }
      function eb({ className: e, ...t }) {
        return (0, r.jsx)(eu.l9, {
          "data-slot": "tabs-trigger",
          className: O(
            "relative inline-flex h-[calc(100%-1px)] flex-1 items-center justify-center gap-1.5 rounded-md border border-transparent px-2 py-1 text-sm font-medium whitespace-nowrap text-foreground/60 transition-all group-data-[orientation=vertical]/tabs:w-full group-data-[orientation=vertical]/tabs:justify-start hover:text-foreground focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-1 focus-visible:outline-ring disabled:pointer-events-none disabled:opacity-50 group-data-[variant=default]/tabs-list:data-[state=active]:shadow-sm group-data-[variant=line]/tabs-list:data-[state=active]:shadow-none dark:text-muted-foreground dark:hover:text-foreground [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
            "group-data-[variant=line]/tabs-list:bg-transparent group-data-[variant=line]/tabs-list:data-[state=active]:bg-transparent dark:group-data-[variant=line]/tabs-list:data-[state=active]:border-transparent dark:group-data-[variant=line]/tabs-list:data-[state=active]:bg-transparent",
            "data-[state=active]:bg-background data-[state=active]:text-foreground dark:data-[state=active]:border-input dark:data-[state=active]:bg-input/30 dark:data-[state=active]:text-foreground",
            "after:absolute after:bg-foreground after:opacity-0 after:transition-opacity group-data-[orientation=horizontal]/tabs:after:inset-x-0 group-data-[orientation=horizontal]/tabs:after:bottom-[-5px] group-data-[orientation=horizontal]/tabs:after:h-0.5 group-data-[orientation=vertical]/tabs:after:inset-y-0 group-data-[orientation=vertical]/tabs:after:-right-1 group-data-[orientation=vertical]/tabs:after:w-0.5 group-data-[variant=line]/tabs-list:data-[state=active]:after:opacity-100",
            e,
          ),
          ...t,
        });
      }
      function eg({ className: e, ...t }) {
        return (0, r.jsx)(eu.UC, {
          "data-slot": "tabs-content",
          className: O("flex-1 outline-none", e),
          ...t,
        });
      }
      let ej = [
          {
            id: "PAY-04821",
            counterparty: "DEMO Counterparty 01",
            scheme: "SCT",
            source: "Corporate Portal",
            channel: "Corporate",
            original: "<STREET> <NUMBER>, <POSTCODE> <TOWN>, DE",
            format: "Unstructured",
            status: "Open",
            confidence: 96,
            amount: 48250,
            currency: "EUR",
            executionDate: "2026-11-16",
            issues: ["Town name not structured", "Country not structured"],
            proposed: {
              street: "Demo Street",
              buildingNumber: "001",
              postCode: "00001",
              town: "Demo City",
              country: "DE",
              addressLine: "",
            },
          },
          {
            id: "PAY-04818",
            counterparty: "DEMO Counterparty 02",
            scheme: "SCT Inst",
            source: "ERP / SFTP",
            channel: "File",
            original: "<NUMBER> <STREET> <POSTCODE> <TOWN> FR",
            format: "Unstructured",
            status: "Needs input",
            confidence: 82,
            amount: 12780,
            currency: "EUR",
            executionDate: "2026-11-17",
            issues: [
              "Address line only",
              "Country value requires confirmation",
            ],
            proposed: {
              street: "Sample Road",
              buildingNumber: "002",
              postCode: "00002",
              town: "Sample City",
              country: "FR",
              addressLine: "",
            },
          },
          {
            id: "PAY-04809",
            counterparty: "DEMO Counterparty 03",
            scheme: "OCT Inst",
            source: "Payment Hub",
            channel: "API",
            original: "<NUMBER> <STREET>, <DISTRICT> / <TOWN> / KR",
            format: "Hybrid",
            status: "Open",
            confidence: 91,
            amount: 8300,
            currency: "EUR",
            executionDate: "2026-11-18",
            issues: [
              "Country present in address line",
              "Review non-SEPA party data",
            ],
            proposed: {
              street: "Example Avenue",
              buildingNumber: "003",
              postCode: "",
              town: "Example City",
              country: "KR",
              addressLine: "Demo District",
            },
          },
          {
            id: "PAY-04796",
            counterparty: "DEMO Counterparty 04",
            scheme: "SDD B2B",
            source: "Mandate Service",
            channel: "Direct debit",
            original: "<STREET> <NUMBER>, <TOWN>, <POSTCODE>, EE",
            format: "Structured",
            status: "Approved",
            confidence: 99,
            amount: 26140,
            currency: "EUR",
            executionDate: "2026-11-20",
            issues: [],
            proposed: {
              street: "Test Lane",
              buildingNumber: "004",
              postCode: "00004",
              town: "Test City",
              country: "EE",
              addressLine: "",
            },
          },
          {
            id: "PAY-04777",
            counterparty: "DEMO Counterparty 05",
            scheme: "SCT",
            source: "ERP / SFTP",
            channel: "File",
            original: "<STREET> <NUMBER> <POSTCODE> <TOWN>",
            format: "Unstructured",
            status: "Open",
            confidence: 88,
            amount: 6910,
            currency: "EUR",
            executionDate: "2026-11-22",
            issues: ["Country missing", "Town name not structured"],
            proposed: {
              street: "Illustration Street",
              buildingNumber: "005",
              postCode: "00005",
              town: "Illustration City",
              country: "IT",
              addressLine: "",
            },
          },
          {
            id: "PAY-04762",
            counterparty: "DEMO Counterparty 06",
            scheme: "SCT Inst",
            source: "Corporate Portal",
            channel: "Corporate",
            original: "<STREET> <NUMBER>, <POSTCODE> <TOWN>, NL",
            format: "Hybrid",
            status: "Approved",
            confidence: 98,
            amount: 19450,
            currency: "EUR",
            executionDate: "2026-11-23",
            issues: [],
            proposed: {
              street: "Placeholder Canal",
              buildingNumber: "006",
              postCode: "00006",
              town: "Placeholder City",
              country: "NL",
              addressLine: "",
            },
          },
          {
            id: "PAY-04741",
            counterparty: "DEMO Counterparty 07",
            scheme: "SDD Core",
            source: "Standing Orders",
            channel: "Retail",
            original: "<STREET> <NUMBER> <TOWN> <COUNTRY>",
            format: "Unstructured",
            status: "Open",
            confidence: 74,
            amount: 1180,
            currency: "EUR",
            executionDate: "2026-12-01",
            issues: ["Postal code missing", "Country not in ISO code format"],
            proposed: {
              street: "Mock Boulevard",
              buildingNumber: "007",
              postCode: "",
              town: "Mock City",
              country: "PL",
              addressLine: "",
            },
          },
          {
            id: "PAY-04726",
            counterparty: "DEMO Counterparty 08",
            scheme: "SCT",
            source: "Payment Hub",
            channel: "API",
            original: "<STREET> <NUMBER>, <TOWN> <POSTCODE>, GR",
            format: "Structured",
            status: "Approved",
            confidence: 99,
            amount: 31200,
            currency: "EUR",
            executionDate: "2026-11-25",
            issues: [],
            proposed: {
              street: "Synthetic Street",
              buildingNumber: "008",
              postCode: "00008",
              town: "Synthetic City",
              country: "GR",
              addressLine: "",
            },
          },
        ],
        ev = `<?xml version="1.0" encoding="UTF-8"?>
<Document>
  <CdtTrfTxInf><Cdtr><Nm>DEMO Counterparty 01</Nm><PstlAdr><AdrLine>STREET NUMBER POSTCODE TOWN DE</AdrLine></PstlAdr></Cdtr></CdtTrfTxInf>
  <CdtTrfTxInf><Cdtr><Nm>DEMO Counterparty 02</Nm><PstlAdr><AdrLine>NUMBER STREET POSTCODE TOWN FR</AdrLine></PstlAdr></Cdtr></CdtTrfTxInf>
  <CdtTrfTxInf><Cdtr><Nm>DEMO Counterparty 06</Nm><PstlAdr><TwnNm>Demo City</TwnNm><Ctry>NL</Ctry><AdrLine>Placeholder Street 006</AdrLine></PstlAdr></Cdtr></CdtTrfTxInf>
  <CdtTrfTxInf><Cdtr><Nm>DEMO Counterparty 08</Nm><PstlAdr><StrtNm>Synthetic Street</StrtNm><BldgNb>008</BldgNb><PstCd>00008</PstCd><TwnNm>Synthetic City</TwnNm><Ctry>GR</Ctry></PstlAdr></Cdtr></CdtTrfTxInf>
  <CdtTrfTxInf><Cdtr><Nm>DEMO Counterparty 05</Nm><PstlAdr><AdrLine>STREET NUMBER POSTCODE TOWN</AdrLine></PstlAdr></Cdtr></CdtTrfTxInf>
</Document>`,
        eN = [
          { name: "Payment Hub", score: 96, volume: "4,420" },
          { name: "Mandate Service", score: 87, volume: "2,160" },
          { name: "Standing Orders", score: 74, volume: "1,945" },
          { name: "Corporate Portal", score: 63, volume: "2,380" },
          { name: "ERP / SFTP", score: 48, volume: "1,935" },
        ],
        ey = {
          Open: "border-amber-200 bg-amber-50 text-amber-800",
          Approved: "border-emerald-200 bg-emerald-50 text-emerald-800",
          "Needs input": "border-sky-200 bg-sky-50 text-sky-800",
          Dismissed: "border-slate-200 bg-slate-50 text-slate-600",
        },
        ew = {
          Unstructured: "border-red-200 bg-red-50 text-red-700",
          Hybrid: "border-amber-200 bg-amber-50 text-amber-800",
          Structured: "border-emerald-200 bg-emerald-50 text-emerald-800",
        };
      function eC(e, t) {
        return new Intl.NumberFormat("en-US", {
          style: "currency",
          currency: t,
          maximumFractionDigits: 0,
        }).format(e);
      }
      function ek(e, t, s) {
        let r = e.toLowerCase().endsWith(".xml") || t.trim().startsWith("<"),
          a = 0,
          n = 0,
          i = 0,
          d = 0;
        if (r) {
          let e = t.match(/<PstlAdr(?:\s[^>]*)?>([\s\S]*?)<\/PstlAdr>/gi) || [];
          ((a =
            e.length ||
            (t.match(RegExp("<CdtTrfTxInf(?:\\s[^>]*)?>", "gi")) || [])
              .length ||
            1),
            e.forEach((e) => {
              let t = /<AdrLine(?:\s[^>]*)?>/i.test(e),
                s = /<TwnNm(?:\s[^>]*)?>[^<]+<\/TwnNm>/i.test(e),
                r = /<Ctry(?:\s[^>]*)?>[^<]+<\/Ctry>/i.test(e);
              (!t || (s && r) || (n += 1), s || (i += 1), r || (d += 1));
            }));
        } else {
          let e = t.trim().split(/\r?\n/).filter(Boolean),
            s = (e.shift() || "")
              .toLowerCase()
              .split(",")
              .map((e) => e.trim());
          a = e.length;
          let r = s.some((e) => /address|adr_line|address_line/.test(e)),
            l = s.some((e) => /town|city/.test(e)),
            o = s.some((e) => /country|ctry/.test(e));
          ((n = !r || (l && o) ? 0 : a), (i = l ? 0 : a), (d = o ? 0 : a));
        }
        return {
          name: e,
          size: s > 1024 ? `${(s / 1024).toFixed(1)} KB` : `${s} B`,
          scanned: a,
          compliant: Math.max(0, a - n),
          unstructured: n,
          missingTown: i,
          missingCountry: d,
          format: r ? "ISO 20022 XML" : "CSV / delimited",
          scannedAt: new Date().toISOString(),
        };
      }
      function eA({ label: e, value: t, note: s, icon: a, accent: n }) {
        return (0, r.jsxs)("article", {
          className:
            "rounded-2xl border border-border bg-card p-5 shadow-[0_8px_30px_rgba(24,53,61,0.05)]",
          children: [
            (0, r.jsxs)("div", {
              className: "flex items-start justify-between gap-4",
              children: [
                (0, r.jsxs)("div", {
                  children: [
                    (0, r.jsx)("p", {
                      className: "text-sm font-medium text-muted-foreground",
                      children: e,
                    }),
                    (0, r.jsx)("p", {
                      className:
                        "mt-2 text-3xl font-semibold tracking-[-0.04em] text-foreground",
                      children: t,
                    }),
                  ],
                }),
                (0, r.jsx)("div", {
                  className: `flex size-10 items-center justify-center rounded-xl ${n}`,
                  children: a,
                }),
              ],
            }),
            (0, r.jsx)("p", {
              className: "mt-4 text-sm text-muted-foreground",
              children: s,
            }),
          ],
        });
      }
      function eS({ eyebrow: e, title: t, description: s, action: a }) {
        return (0, r.jsxs)("div", {
          className:
            "flex flex-col justify-between gap-4 sm:flex-row sm:items-end",
          children: [
            (0, r.jsxs)("div", {
              children: [
                (0, r.jsx)("p", {
                  className:
                    "text-xs font-semibold uppercase tracking-[0.18em] text-primary",
                  children: e,
                }),
                (0, r.jsx)("h1", {
                  className:
                    "mt-2 text-2xl font-semibold tracking-[-0.035em] text-foreground sm:text-3xl",
                  children: t,
                }),
                (0, r.jsx)("p", {
                  className:
                    "mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base",
                  children: s,
                }),
              ],
            }),
            a,
          ],
        });
      }
      function ez() {
        let [e, t] = (0, a.useState)("overview"),
          [s, R] = (0, a.useState)(ej),
          [_, L] = (0, a.useState)(null),
          [D, O] = (0, a.useState)(null),
          [E, F] = (0, a.useState)("all"),
          [M, V] = (0, a.useState)("all"),
          [$, Y] = (0, a.useState)(""),
          [W, X] = (0, a.useState)(null),
          [Z, ee] = (0, a.useState)(!1),
          [et, eu] = (0, a.useState)("all"),
          [eh, ez] = (0, a.useState)("current"),
          [eT, eP] = (0, a.useState)(null),
          [eR, e_] = (0, a.useState)(""),
          [eL, eD] = (0, a.useState)(!1),
          eO = (0, a.useRef)(null),
          eE = Math.max(
            0,
            Math.ceil(
              (new Date("2026-11-15T00:00:00Z").getTime() - Date.now()) / 864e5,
            ),
          );
        ((0, a.useEffect)(() => {
          let e = window.requestAnimationFrame(() => {
            try {
              let e = localStorage.getItem("paydata-records-v1"),
                t = localStorage.getItem("paydata-scan-v1");
              (e && R(JSON.parse(e)), t && X(JSON.parse(t)));
            } catch {}
          });
          return () => window.cancelAnimationFrame(e);
        }, []),
          (0, a.useEffect)(() => {
            try {
              localStorage.setItem("paydata-records-v1", JSON.stringify(s));
            } catch {}
          }, [s]));
        let eI = s.filter((e) => "Approved" === e.status).length,
          eF = s.filter(
            (e) => "Open" === e.status || "Needs input" === e.status,
          ).length,
          eU = Math.round(((s.length - eF) / s.length) * 100),
          eB = Math.max(96, 1824 - 214 * eI),
          eM = (0, a.useMemo)(() => {
            let e = $.trim().toLowerCase();
            return s.filter(
              (t) =>
                (!e ||
                  t.id.toLowerCase().includes(e) ||
                  t.counterparty.toLowerCase().includes(e) ||
                  t.source.toLowerCase().includes(e)) &&
                ("all" === E || t.status === E) &&
                ("all" === M || t.format === M),
            );
          }, [s, $, E, M]);
        function eV(e) {
          (e_(e), window.setTimeout(() => e_(""), 2800));
        }
        function e$(e) {
          (L(e), O({ ...e.proposed }));
        }
        function eH(e) {
          _ &&
            (R((t) =>
              t.map((t) =>
                t.id === _.id
                  ? {
                      ...t,
                      status: e,
                      format: "Approved" === e ? "Structured" : t.format,
                      issues: "Approved" === e ? [] : t.issues,
                      proposed: D || t.proposed,
                    }
                  : t,
              ),
            ),
            L(null),
            O(null),
            eV(
              "Approved" === e
                ? "Correction approved and saved locally"
                : `Record marked ${e.toLowerCase()}`,
            ));
        }
        async function eq(e) {
          let t = await e.text(),
            s = ek(e.name, t, e.size);
          X(s);
          try {
            localStorage.setItem("paydata-scan-v1", JSON.stringify(s));
          } catch {}
          eV(
            `${s.scanned} payment address${1 === s.scanned ? "" : "es"} analyzed`,
          );
        }
        return (0, r.jsxs)("main", {
          className: "min-h-screen bg-background text-foreground",
          children: [
            (0, r.jsxs)(ep, {
              value: e,
              onValueChange: t,
              className:
                "min-h-screen gap-0 lg:grid lg:grid-cols-[248px_minmax(0,1fr)]",
              children: [
                (0, r.jsxs)("aside", {
                  className:
                    "bg-sidebar text-sidebar-foreground lg:sticky lg:top-0 lg:flex lg:h-screen lg:flex-col",
                  children: [
                    (0, r.jsxs)("div", {
                      className:
                        "flex items-center justify-between border-b border-sidebar-border px-5 py-5 lg:block lg:border-b-0 lg:px-6 lg:pt-7",
                      children: [
                        (0, r.jsxs)("div", {
                          className: "flex items-center gap-3",
                          children: [
                            (0, r.jsx)("div", {
                              className:
                                "flex size-10 items-center justify-center rounded-xl bg-sidebar-primary text-sidebar-primary-foreground shadow-[0_8px_25px_rgba(40,182,167,0.2)]",
                              children: (0, r.jsx)(n.A, {
                                className: "size-5",
                              }),
                            }),
                            (0, r.jsxs)("div", {
                              children: [
                                (0, r.jsx)("p", {
                                  className:
                                    "text-[13px] font-semibold uppercase tracking-[0.16em] text-[#76d7cc]",
                                  children: "FinTech Tomorrow",
                                }),
                                (0, r.jsx)("p", {
                                  className:
                                    "mt-0.5 text-sm font-medium text-white",
                                  children: "PayData Control",
                                }),
                              ],
                            }),
                          ],
                        }),
                        (0, r.jsx)(U, {
                          variant: "ghost",
                          size: "icon-sm",
                          className:
                            "text-sidebar-foreground hover:bg-sidebar-accent hover:text-white lg:hidden",
                          onClick: () => eD(!0),
                          "aria-label": "Open user guide",
                          children: (0, r.jsx)(i.A, {}),
                        }),
                      ],
                    }),
                    (0, r.jsxs)(ef, {
                      variant: "line",
                      className:
                        "scrollbar-none flex w-full flex-row justify-start gap-1 overflow-x-auto rounded-none border-b border-sidebar-border bg-transparent px-3 py-3 lg:mt-7 lg:flex-col lg:overflow-visible lg:border-b-0 lg:px-4",
                      children: [
                        (0, r.jsxs)(eb, {
                          value: "overview",
                          className:
                            "h-10 min-w-max rounded-lg px-3 text-[#b9ced3] hover:bg-sidebar-accent hover:text-white data-[state=active]:bg-sidebar-accent data-[state=active]:text-white",
                          children: [(0, r.jsx)(d.A, {}), " Overview"],
                        }),
                        (0, r.jsxs)(eb, {
                          value: "analyze",
                          className:
                            "h-10 min-w-max rounded-lg px-3 text-[#b9ced3] hover:bg-sidebar-accent hover:text-white data-[state=active]:bg-sidebar-accent data-[state=active]:text-white",
                          children: [(0, r.jsx)(l.A, {}), " File analyzer"],
                        }),
                        (0, r.jsxs)(eb, {
                          value: "remediation",
                          className:
                            "h-10 min-w-max rounded-lg px-3 text-[#b9ced3] hover:bg-sidebar-accent hover:text-white data-[state=active]:bg-sidebar-accent data-[state=active]:text-white",
                          children: [
                            (0, r.jsx)(o.A, {}),
                            " Remediation",
                            eF > 0 &&
                              (0, r.jsx)("span", {
                                className:
                                  "ml-auto rounded-full bg-[#f3b35a] px-1.5 py-0.5 text-[11px] font-bold text-[#2e261b]",
                                children: eF,
                              }),
                          ],
                        }),
                        (0, r.jsxs)(eb, {
                          value: "cutover",
                          className:
                            "h-10 min-w-max rounded-lg px-3 text-[#b9ced3] hover:bg-sidebar-accent hover:text-white data-[state=active]:bg-sidebar-accent data-[state=active]:text-white",
                          children: [(0, r.jsx)(c.A, {}), " Cutover lab"],
                        }),
                      ],
                    }),
                    (0, r.jsx)("div", {
                      className: "mt-auto hidden px-5 pb-6 lg:block",
                      children: (0, r.jsxs)("div", {
                        className:
                          "rounded-2xl border border-[#2b4e57] bg-[#14333d] p-4",
                        children: [
                          (0, r.jsxs)("div", {
                            className:
                              "flex items-center gap-2 text-sm font-medium text-white",
                            children: [
                              (0, r.jsx)(m.A, {
                                className: "size-4 text-[#76d7cc]",
                              }),
                              " Local workspace",
                            ],
                          }),
                          (0, r.jsx)("p", {
                            className: "mt-2 text-xs leading-5 text-[#9bb8be]",
                            children:
                              "Files and review decisions stay in this browser. No server storage is used.",
                          }),
                        ],
                      }),
                    }),
                  ],
                }),
                (0, r.jsxs)("section", {
                  className: "min-w-0 paper-grid",
                  children: [
                    (0, r.jsx)("header", {
                      className:
                        "border-b border-border bg-white/90 backdrop-blur",
                      children: (0, r.jsxs)("div", {
                        className:
                          "flex min-h-[72px] items-center justify-between gap-4 px-4 sm:px-6 lg:px-8",
                        children: [
                          (0, r.jsxs)("div", {
                            className: "min-w-0",
                            children: [
                              (0, r.jsx)("p", {
                                className:
                                  "truncate text-sm font-semibold text-foreground",
                                children:
                                  "Payment Data Readiness & Remediation",
                              }),
                              (0, r.jsxs)("div", {
                                className:
                                  "mt-1 flex items-center gap-2 text-xs text-muted-foreground",
                                children: [
                                  (0, r.jsxs)("span", {
                                    className: "inline-flex items-center gap-1",
                                    children: [
                                      (0, r.jsx)(x.A, {
                                        className: "size-3.5",
                                      }),
                                      " Local mode",
                                    ],
                                  }),
                                  (0, r.jsx)("span", {
                                    "aria-hidden": "true",
                                    children: "•",
                                  }),
                                  (0, r.jsx)("span", {
                                    children: "Demo portfolio",
                                  }),
                                ],
                              }),
                            ],
                          }),
                          (0, r.jsxs)("div", {
                            className: "flex shrink-0 items-center gap-2",
                            children: [
                              (0, r.jsxs)(U, {
                                variant: "outline",
                                size: "sm",
                                className: "hidden bg-white sm:inline-flex",
                                onClick: () => eD(!0),
                                children: [(0, r.jsx)(i.A, {}), " Guide"],
                              }),
                              (0, r.jsxs)(U, {
                                size: "sm",
                                onClick: function () {
                                  let e = new Blob(
                                      [
                                        JSON.stringify(
                                          {
                                            generatedAt:
                                              new Date().toISOString(),
                                            planningBasis:
                                              "EPC timeline remains 15 November 2026 until further notice",
                                            readiness: eU,
                                            openRemediation: eF,
                                            approvedCorrections: eI,
                                            latestScan: W,
                                            remediationRecords: s,
                                          },
                                          null,
                                          2,
                                        ),
                                      ],
                                      { type: "application/json" },
                                    ),
                                    t = URL.createObjectURL(e),
                                    r = document.createElement("a");
                                  ((r.href = t),
                                    (r.download = `payment-data-readiness-${new Date().toISOString().slice(0, 10)}.json`),
                                    r.click(),
                                    URL.revokeObjectURL(t),
                                    eV("Readiness report exported"));
                                },
                                children: [
                                  (0, r.jsx)(u.A, {}),
                                  " ",
                                  (0, r.jsx)("span", {
                                    className: "hidden sm:inline",
                                    children: "Export report",
                                  }),
                                  (0, r.jsx)("span", {
                                    className: "sm:hidden",
                                    children: "Export",
                                  }),
                                ],
                              }),
                            ],
                          }),
                        ],
                      }),
                    }),
                    (0, r.jsx)("div", {
                      className: "px-4 py-6 sm:px-6 lg:px-8 lg:py-8",
                      children: (0, r.jsxs)("div", {
                        className: "mx-auto max-w-[1380px]",
                        children: [
                          (0, r.jsxs)(eg, {
                            value: "overview",
                            className: "soft-enter space-y-6",
                            children: [
                              (0, r.jsx)(eS, {
                                eyebrow: "Readiness control tower",
                                title: "Know what will fail before cutover",
                                description:
                                  "Prioritize the source systems, payment files, and counterparties that still depend on unstructured postal addresses.",
                                action: (0, r.jsxs)(U, {
                                  variant: "outline",
                                  className: "bg-white",
                                  onClick: () => t("analyze"),
                                  children: [
                                    (0, r.jsx)(p.A, {}),
                                    " Analyze a file",
                                  ],
                                }),
                              }),
                              (0, r.jsxs)("div", {
                                className:
                                  "flex flex-col gap-3 rounded-2xl border border-amber-200 bg-[#fff9ec] p-4 sm:flex-row sm:items-center sm:justify-between",
                                children: [
                                  (0, r.jsxs)("div", {
                                    className: "flex gap-3",
                                    children: [
                                      (0, r.jsx)("div", {
                                        className:
                                          "mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-800",
                                        children: (0, r.jsx)(h.A, {
                                          className: "size-4",
                                        }),
                                      }),
                                      (0, r.jsxs)("div", {
                                        children: [
                                          (0, r.jsx)("p", {
                                            className:
                                              "text-sm font-semibold text-[#624515]",
                                            children:
                                              "Current EPC planning basis: 15 November 2026",
                                          }),
                                          (0, r.jsx)("p", {
                                            className:
                                              "mt-1 text-sm leading-5 text-[#7b633d]",
                                            children:
                                              "EPC will review its position on 9 September 2026. Until further notice, preparations should continue against the existing date.",
                                          }),
                                        ],
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)("a", {
                                    href: "https://www.europeanpaymentscouncil.eu/news-insights/news/november-2026-end-date-unstructured-address-format-epc-payment-scheme",
                                    target: "_blank",
                                    rel: "noreferrer",
                                    className:
                                      "ml-12 inline-flex shrink-0 items-center gap-1 text-sm font-semibold text-[#76500c] hover:underline sm:ml-0",
                                    children: [
                                      "EPC update ",
                                      (0, r.jsx)(f.A, { className: "size-4" }),
                                    ],
                                  }),
                                ],
                              }),
                              (0, r.jsxs)("div", {
                                className:
                                  "grid gap-4 sm:grid-cols-2 xl:grid-cols-4",
                                children: [
                                  (0, r.jsx)(eA, {
                                    label: "Portfolio readiness",
                                    value: `${eU}%`,
                                    note: `${eI} reviewed records verified`,
                                    icon: (0, r.jsx)(b.A, {
                                      className: "size-5",
                                    }),
                                    accent: "bg-emerald-50 text-emerald-700",
                                  }),
                                  (0, r.jsx)(eA, {
                                    label: "Payments at risk",
                                    value: eB.toLocaleString(),
                                    note: "Projected after current approvals",
                                    icon: (0, r.jsx)(g.A, {
                                      className: "size-5",
                                    }),
                                    accent: "bg-red-50 text-red-700",
                                  }),
                                  (0, r.jsx)(eA, {
                                    label: "Open remediation",
                                    value: "342",
                                    note: `${eF} priority samples in this workspace`,
                                    icon: (0, r.jsx)(o.A, {
                                      className: "size-5",
                                    }),
                                    accent: "bg-amber-50 text-amber-700",
                                  }),
                                  (0, r.jsx)(eA, {
                                    label: "Time to planning date",
                                    value: `${eE} days`,
                                    note: "Rule-watch status is shown above",
                                    icon: (0, r.jsx)(h.A, {
                                      className: "size-5",
                                    }),
                                    accent: "bg-sky-50 text-sky-700",
                                  }),
                                ],
                              }),
                              (0, r.jsxs)("div", {
                                className:
                                  "grid gap-5 xl:grid-cols-[1.3fr_0.7fr]",
                                children: [
                                  (0, r.jsxs)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-card p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: [
                                      (0, r.jsxs)("div", {
                                        className:
                                          "flex items-start justify-between gap-4",
                                        children: [
                                          (0, r.jsxs)("div", {
                                            children: [
                                              (0, r.jsx)("h2", {
                                                className:
                                                  "text-lg font-semibold tracking-[-0.02em]",
                                                children:
                                                  "Readiness by source system",
                                              }),
                                              (0, r.jsx)("p", {
                                                className:
                                                  "mt-1 text-sm text-muted-foreground",
                                                children:
                                                  "Estimated compliance across 12,840 payment records",
                                              }),
                                            ],
                                          }),
                                          (0, r.jsx)(I, {
                                            variant: "outline",
                                            className:
                                              "border-border bg-muted/50 text-muted-foreground",
                                            children: "Last scan \xb7 today",
                                          }),
                                        ],
                                      }),
                                      (0, r.jsx)("div", {
                                        className: "mt-6 space-y-5",
                                        children: eN.map((e) =>
                                          (0, r.jsxs)(
                                            "button",
                                            {
                                              className:
                                                "group block w-full text-left",
                                              onClick: () => {
                                                (Y(e.name), t("remediation"));
                                              },
                                              children: [
                                                (0, r.jsxs)("div", {
                                                  className:
                                                    "mb-2 flex items-center justify-between gap-4 text-sm",
                                                  children: [
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "font-medium group-hover:text-primary",
                                                      children: e.name,
                                                    }),
                                                    (0, r.jsxs)("span", {
                                                      className:
                                                        "text-muted-foreground",
                                                      children: [
                                                        (0, r.jsxs)("span", {
                                                          className:
                                                            "font-semibold text-foreground",
                                                          children: [
                                                            e.score,
                                                            "%",
                                                          ],
                                                        }),
                                                        " \xb7 ",
                                                        e.volume,
                                                        " records",
                                                      ],
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsx)("div", {
                                                  className:
                                                    "h-2.5 overflow-hidden rounded-full bg-[#e8eeef]",
                                                  children: (0, r.jsx)("div", {
                                                    className: `h-full rounded-full ${e.score >= 85 ? "bg-[#168b7f]" : e.score >= 65 ? "bg-[#e0a24c]" : "bg-[#d45e4b]"}`,
                                                    style: {
                                                      width: `${e.score}%`,
                                                    },
                                                  }),
                                                }),
                                              ],
                                            },
                                            e.name,
                                          ),
                                        ),
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-card p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: [
                                      (0, r.jsxs)("div", {
                                        children: [
                                          (0, r.jsx)("h2", {
                                            className:
                                              "text-lg font-semibold tracking-[-0.02em]",
                                            children: "Readiness mix",
                                          }),
                                          (0, r.jsx)("p", {
                                            className:
                                              "mt-1 text-sm text-muted-foreground",
                                            children:
                                              "Current modeled portfolio",
                                          }),
                                        ],
                                      }),
                                      (0, r.jsxs)("div", {
                                        className:
                                          "mt-6 flex flex-col items-center gap-6 sm:flex-row xl:flex-col 2xl:flex-row",
                                        children: [
                                          (0, r.jsx)("div", {
                                            className:
                                              "readiness-ring relative flex size-40 shrink-0 items-center justify-center rounded-full",
                                            style: { "--readiness": `${eU}%` },
                                            children: (0, r.jsxs)("div", {
                                              className:
                                                "flex size-[120px] flex-col items-center justify-center rounded-full bg-white shadow-inner",
                                              children: [
                                                (0, r.jsxs)("span", {
                                                  className:
                                                    "text-3xl font-semibold tracking-[-0.05em]",
                                                  children: [eU, "%"],
                                                }),
                                                (0, r.jsx)("span", {
                                                  className:
                                                    "mt-1 text-xs text-muted-foreground",
                                                  children: "ready",
                                                }),
                                              ],
                                            }),
                                          }),
                                          (0, r.jsxs)("div", {
                                            className:
                                              "w-full space-y-3 text-sm",
                                            children: [
                                              (0, r.jsxs)("div", {
                                                className:
                                                  "flex items-center justify-between gap-4",
                                                children: [
                                                  (0, r.jsxs)("span", {
                                                    className:
                                                      "flex items-center gap-2 text-muted-foreground",
                                                    children: [
                                                      (0, r.jsx)("i", {
                                                        className:
                                                          "size-2.5 rounded-full bg-[#168b7f]",
                                                      }),
                                                      " Structured",
                                                    ],
                                                  }),
                                                  (0, r.jsx)("strong", {
                                                    children: "6,210",
                                                  }),
                                                ],
                                              }),
                                              (0, r.jsxs)("div", {
                                                className:
                                                  "flex items-center justify-between gap-4",
                                                children: [
                                                  (0, r.jsxs)("span", {
                                                    className:
                                                      "flex items-center gap-2 text-muted-foreground",
                                                    children: [
                                                      (0, r.jsx)("i", {
                                                        className:
                                                          "size-2.5 rounded-full bg-[#e0a24c]",
                                                      }),
                                                      " Hybrid",
                                                    ],
                                                  }),
                                                  (0, r.jsx)("strong", {
                                                    children: "4,806",
                                                  }),
                                                ],
                                              }),
                                              (0, r.jsxs)("div", {
                                                className:
                                                  "flex items-center justify-between gap-4",
                                                children: [
                                                  (0, r.jsxs)("span", {
                                                    className:
                                                      "flex items-center gap-2 text-muted-foreground",
                                                    children: [
                                                      (0, r.jsx)("i", {
                                                        className:
                                                          "size-2.5 rounded-full bg-[#d45e4b]",
                                                      }),
                                                      " Unstructured",
                                                    ],
                                                  }),
                                                  (0, r.jsx)("strong", {
                                                    children: "1,824",
                                                  }),
                                                ],
                                              }),
                                              (0, r.jsx)("div", {
                                                className:
                                                  "border-t border-border pt-3 text-xs leading-5 text-muted-foreground",
                                                children:
                                                  "Select a source-system bar to open its remediation items.",
                                              }),
                                            ],
                                          }),
                                        ],
                                      }),
                                    ],
                                  }),
                                ],
                              }),
                              (0, r.jsxs)("div", {
                                className: "grid gap-5 lg:grid-cols-2",
                                children: [
                                  (0, r.jsxs)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-card p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: [
                                      (0, r.jsxs)("div", {
                                        className:
                                          "flex items-center justify-between",
                                        children: [
                                          (0, r.jsxs)("div", {
                                            children: [
                                              (0, r.jsx)("h2", {
                                                className:
                                                  "text-lg font-semibold tracking-[-0.02em]",
                                                children:
                                                  "Highest-impact issues",
                                              }),
                                              (0, r.jsx)("p", {
                                                className:
                                                  "mt-1 text-sm text-muted-foreground",
                                                children:
                                                  "Ranked by projected rejected volume",
                                              }),
                                            ],
                                          }),
                                          (0, r.jsx)(j.A, {
                                            className:
                                              "size-5 text-muted-foreground",
                                          }),
                                        ],
                                      }),
                                      (0, r.jsx)("div", {
                                        className:
                                          "mt-5 divide-y divide-border",
                                        children: [
                                          [
                                            "Town name missing from structured fields",
                                            "886",
                                            "Corporate Portal",
                                          ],
                                          [
                                            "Country embedded in address line",
                                            "571",
                                            "ERP / SFTP",
                                          ],
                                          [
                                            "Address line only",
                                            "367",
                                            "Standing Orders",
                                          ],
                                        ].map(([e, s, a]) =>
                                          (0, r.jsxs)(
                                            "button",
                                            {
                                              className:
                                                "flex w-full items-center gap-4 py-4 text-left",
                                              onClick: () => t("remediation"),
                                              children: [
                                                (0, r.jsx)("span", {
                                                  className:
                                                    "flex size-9 shrink-0 items-center justify-center rounded-xl bg-red-50 text-red-700",
                                                  children: (0, r.jsx)(g.A, {
                                                    className: "size-4",
                                                  }),
                                                }),
                                                (0, r.jsxs)("span", {
                                                  className: "min-w-0 flex-1",
                                                  children: [
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "block truncate text-sm font-medium",
                                                      children: e,
                                                    }),
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "mt-1 block text-xs text-muted-foreground",
                                                      children: a,
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsxs)("span", {
                                                  className: "text-right",
                                                  children: [
                                                    (0, r.jsx)("strong", {
                                                      className:
                                                        "block text-sm",
                                                      children: s,
                                                    }),
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "text-xs text-muted-foreground",
                                                      children: "at risk",
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsx)(v.A, {
                                                  className:
                                                    "size-4 text-muted-foreground",
                                                }),
                                              ],
                                            },
                                            e,
                                          ),
                                        ),
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-card p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: [
                                      (0, r.jsxs)("div", {
                                        className:
                                          "flex items-center justify-between",
                                        children: [
                                          (0, r.jsxs)("div", {
                                            children: [
                                              (0, r.jsx)("h2", {
                                                className:
                                                  "text-lg font-semibold tracking-[-0.02em]",
                                                children: "Next best actions",
                                              }),
                                              (0, r.jsx)("p", {
                                                className:
                                                  "mt-1 text-sm text-muted-foreground",
                                                children:
                                                  "Ordered by risk reduction",
                                              }),
                                            ],
                                          }),
                                          (0, r.jsx)(N.A, {
                                            className:
                                              "size-5 text-muted-foreground",
                                          }),
                                        ],
                                      }),
                                      (0, r.jsx)("div", {
                                        className: "mt-5 space-y-3",
                                        children: [
                                          [
                                            "1",
                                            "Review 4 high-confidence proposals",
                                            "Potentially remove 856 projected failures",
                                            "remediation",
                                            "bg-amber-50 text-amber-800",
                                          ],
                                          [
                                            "2",
                                            "Retest the November corporate batch",
                                            "Validate updated ERP mapping before UAT",
                                            "analyze",
                                            "bg-sky-50 text-sky-800",
                                          ],
                                          [
                                            "3",
                                            "Run post-remediation cutover scenario",
                                            "Quantify residual rejection exposure",
                                            "cutover",
                                            "bg-emerald-50 text-emerald-800",
                                          ],
                                        ].map(([e, s, a, n, i]) =>
                                          (0, r.jsxs)(
                                            "button",
                                            {
                                              onClick: () => t(n),
                                              className:
                                                "flex w-full items-center gap-3 rounded-xl border border-border p-3 text-left hover:border-primary/40 hover:bg-accent/40",
                                              children: [
                                                (0, r.jsx)("span", {
                                                  className: `flex size-8 items-center justify-center rounded-lg text-sm font-bold ${i}`,
                                                  children: e,
                                                }),
                                                (0, r.jsxs)("span", {
                                                  className: "flex-1",
                                                  children: [
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "block text-sm font-medium",
                                                      children: s,
                                                    }),
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "text-xs text-muted-foreground",
                                                      children: a,
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsx)(v.A, {
                                                  className:
                                                    "size-4 text-muted-foreground",
                                                }),
                                              ],
                                            },
                                            e,
                                          ),
                                        ),
                                      }),
                                    ],
                                  }),
                                ],
                              }),
                            ],
                          }),
                          (0, r.jsxs)(eg, {
                            value: "analyze",
                            className: "soft-enter space-y-6",
                            children: [
                              (0, r.jsx)(eS, {
                                eyebrow: "Browser-based validation",
                                title: "Analyze a payment file",
                                description:
                                  "Inspect ISO 20022 XML or CSV exports for unstructured postal addresses. File content is processed only in this browser.",
                              }),
                              (0, r.jsxs)("div", {
                                className: `rounded-3xl border-2 border-dashed p-7 text-center transition sm:p-12 ${Z ? "border-primary bg-accent" : "border-[#b8c9cd] bg-white"}`,
                                onDragOver: (e) => {
                                  (e.preventDefault(), ee(!0));
                                },
                                onDragLeave: () => ee(!1),
                                onDrop: function (e) {
                                  (e.preventDefault(), ee(!1));
                                  let t = e.dataTransfer.files?.[0];
                                  t && eq(t);
                                },
                                children: [
                                  (0, r.jsx)("input", {
                                    ref: eO,
                                    type: "file",
                                    accept: ".xml,.csv,.txt,text/xml,text/csv",
                                    className: "hidden",
                                    onChange: function (e) {
                                      let t = e.target.files?.[0];
                                      (t && eq(t), (e.target.value = ""));
                                    },
                                  }),
                                  (0, r.jsx)("div", {
                                    className:
                                      "mx-auto flex size-14 items-center justify-center rounded-2xl bg-secondary text-primary",
                                    children: (0, r.jsx)(y.A, {
                                      className: "size-6",
                                    }),
                                  }),
                                  (0, r.jsx)("h2", {
                                    className:
                                      "mt-5 text-xl font-semibold tracking-[-0.02em]",
                                    children: "Drop a payment file here",
                                  }),
                                  (0, r.jsx)("p", {
                                    className:
                                      "mx-auto mt-2 max-w-lg text-sm leading-6 text-muted-foreground",
                                    children:
                                      "Supported for this MVP: pain.001, pain.008, pacs.008, pacs.003 XML and CSV-style beneficiary exports.",
                                  }),
                                  (0, r.jsxs)("div", {
                                    className:
                                      "mt-6 flex flex-col items-center justify-center gap-3 sm:flex-row",
                                    children: [
                                      (0, r.jsxs)(U, {
                                        onClick: () => eO.current?.click(),
                                        children: [
                                          (0, r.jsx)(p.A, {}),
                                          " Choose file",
                                        ],
                                      }),
                                      (0, r.jsxs)(U, {
                                        variant: "outline",
                                        className: "bg-white",
                                        onClick: function () {
                                          let e = ek(
                                            "corporate_payments_november.xml",
                                            ev,
                                            new Blob([ev]).size,
                                          );
                                          X(e);
                                          try {
                                            localStorage.setItem(
                                              "paydata-scan-v1",
                                              JSON.stringify(e),
                                            );
                                          } catch {}
                                          eV("Sample ISO 20022 file analyzed");
                                        },
                                        children: [
                                          (0, r.jsx)(w.A, {}),
                                          " Try sample file",
                                        ],
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)("div", {
                                    className:
                                      "mt-6 inline-flex items-center gap-2 rounded-full bg-muted px-3 py-1.5 text-xs text-muted-foreground",
                                    children: [
                                      (0, r.jsx)(m.A, {
                                        className: "size-3.5",
                                      }),
                                      " Nothing is uploaded to a server",
                                    ],
                                  }),
                                ],
                              }),
                              W
                                ? (0, r.jsxs)("div", {
                                    className: "space-y-5",
                                    children: [
                                      (0, r.jsxs)("article", {
                                        className:
                                          "rounded-2xl border border-border bg-white p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                        children: [
                                          (0, r.jsxs)("div", {
                                            className:
                                              "flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between",
                                            children: [
                                              (0, r.jsxs)("div", {
                                                className:
                                                  "flex min-w-0 items-center gap-3",
                                                children: [
                                                  (0, r.jsx)("span", {
                                                    className:
                                                      "flex size-11 shrink-0 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700",
                                                    children: (0, r.jsx)(n.A, {
                                                      className: "size-5",
                                                    }),
                                                  }),
                                                  (0, r.jsxs)("div", {
                                                    className: "min-w-0",
                                                    children: [
                                                      (0, r.jsx)("h2", {
                                                        className:
                                                          "truncate font-semibold",
                                                        children: W.name,
                                                      }),
                                                      (0, r.jsxs)("p", {
                                                        className:
                                                          "mt-1 text-xs text-muted-foreground",
                                                        children: [
                                                          W.format,
                                                          " \xb7 ",
                                                          W.size,
                                                          " \xb7 analyzed locally",
                                                        ],
                                                      }),
                                                    ],
                                                  }),
                                                ],
                                              }),
                                              (0, r.jsx)(I, {
                                                variant: "outline",
                                                className:
                                                  "border-emerald-200 bg-emerald-50 text-emerald-800",
                                                children: "Analysis complete",
                                              }),
                                            ],
                                          }),
                                          (0, r.jsx)("div", {
                                            className:
                                              "mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-5",
                                            children: [
                                              [
                                                "Addresses scanned",
                                                W.scanned,
                                                "text-foreground",
                                              ],
                                              [
                                                "Compliant",
                                                W.compliant,
                                                "text-emerald-700",
                                              ],
                                              [
                                                "Unstructured",
                                                W.unstructured,
                                                "text-red-700",
                                              ],
                                              [
                                                "Town missing",
                                                W.missingTown,
                                                "text-amber-700",
                                              ],
                                              [
                                                "Country missing",
                                                W.missingCountry,
                                                "text-amber-700",
                                              ],
                                            ].map(([e, t, s]) =>
                                              (0, r.jsxs)(
                                                "div",
                                                {
                                                  className:
                                                    "rounded-xl border border-border bg-[#f9fbfb] p-4",
                                                  children: [
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "text-xs font-medium text-muted-foreground",
                                                      children: e,
                                                    }),
                                                    (0, r.jsx)("p", {
                                                      className: `mt-2 text-2xl font-semibold ${s}`,
                                                      children: t,
                                                    }),
                                                  ],
                                                },
                                                String(e),
                                              ),
                                            ),
                                          }),
                                        ],
                                      }),
                                      (0, r.jsxs)("div", {
                                        className:
                                          "grid gap-5 lg:grid-cols-[1fr_0.8fr]",
                                        children: [
                                          (0, r.jsxs)("article", {
                                            className:
                                              "rounded-2xl border border-border bg-white p-5 sm:p-6",
                                            children: [
                                              (0, r.jsx)("h2", {
                                                className:
                                                  "text-lg font-semibold",
                                                children: "Validation findings",
                                              }),
                                              (0, r.jsx)("div", {
                                                className: "mt-5 space-y-3",
                                                children: [
                                                  {
                                                    label:
                                                      "PA-001 \xb7 Unstructured postal address",
                                                    value: W.unstructured,
                                                    tone: "red",
                                                  },
                                                  {
                                                    label:
                                                      "PA-014 \xb7 Town name missing",
                                                    value: W.missingTown,
                                                    tone: "amber",
                                                  },
                                                  {
                                                    label:
                                                      "PA-015 \xb7 Country missing",
                                                    value: W.missingCountry,
                                                    tone: "amber",
                                                  },
                                                ].map((e) =>
                                                  (0, r.jsxs)(
                                                    "div",
                                                    {
                                                      className:
                                                        "flex items-center gap-3 rounded-xl border border-border p-3",
                                                      children: [
                                                        (0, r.jsx)("span", {
                                                          className: `flex size-8 items-center justify-center rounded-lg ${"red" === e.tone ? "bg-red-50 text-red-700" : "bg-amber-50 text-amber-700"}`,
                                                          children: (0, r.jsx)(
                                                            g.A,
                                                            {
                                                              className:
                                                                "size-4",
                                                            },
                                                          ),
                                                        }),
                                                        (0, r.jsx)("span", {
                                                          className:
                                                            "flex-1 text-sm font-medium",
                                                          children: e.label,
                                                        }),
                                                        (0, r.jsx)(I, {
                                                          variant: "outline",
                                                          className: "bg-white",
                                                          children: e.value,
                                                        }),
                                                      ],
                                                    },
                                                    e.label,
                                                  ),
                                                ),
                                              }),
                                            ],
                                          }),
                                          (0, r.jsxs)("article", {
                                            className:
                                              "rounded-2xl border border-[#b9ded8] bg-[#effaf8] p-5 sm:p-6",
                                            children: [
                                              (0, r.jsx)("h2", {
                                                className:
                                                  "text-lg font-semibold text-[#164a46]",
                                                children:
                                                  "Recommended next step",
                                              }),
                                              (0, r.jsx)("p", {
                                                className:
                                                  "mt-3 text-sm leading-6 text-[#436d69]",
                                                children:
                                                  "Send unstructured records to the review queue. High-confidence proposals can be verified quickly; incomplete records should be returned to the data owner.",
                                              }),
                                              (0, r.jsxs)(U, {
                                                className: "mt-6",
                                                onClick: () => t("remediation"),
                                                children: [
                                                  "Open remediation queue ",
                                                  (0, r.jsx)(f.A, {}),
                                                ],
                                              }),
                                            ],
                                          }),
                                        ],
                                      }),
                                    ],
                                  })
                                : (0, r.jsx)("div", {
                                    className: "grid gap-4 sm:grid-cols-3",
                                    children: [
                                      [
                                        "1",
                                        "Select a source file",
                                        "Use an ISO 20022 XML file or beneficiary CSV export.",
                                      ],
                                      [
                                        "2",
                                        "Run deterministic checks",
                                        "The demo inspects address blocks, town and country fields.",
                                      ],
                                      [
                                        "3",
                                        "Review proposed fixes",
                                        "No address change is applied without a human decision.",
                                      ],
                                    ].map(([e, t, s]) =>
                                      (0, r.jsxs)(
                                        "article",
                                        {
                                          className:
                                            "rounded-2xl border border-border bg-white p-5",
                                          children: [
                                            (0, r.jsx)("span", {
                                              className:
                                                "flex size-8 items-center justify-center rounded-lg bg-secondary text-sm font-bold text-primary",
                                              children: e,
                                            }),
                                            (0, r.jsx)("h3", {
                                              className: "mt-4 font-semibold",
                                              children: t,
                                            }),
                                            (0, r.jsx)("p", {
                                              className:
                                                "mt-2 text-sm leading-6 text-muted-foreground",
                                              children: s,
                                            }),
                                          ],
                                        },
                                        e,
                                      ),
                                    ),
                                  }),
                            ],
                          }),
                          (0, r.jsxs)(eg, {
                            value: "remediation",
                            className: "soft-enter space-y-6",
                            children: [
                              (0, r.jsx)(eS, {
                                eyebrow: "Human-controlled corrections",
                                title: "Remediation queue",
                                description:
                                  "Review proposed structured addresses, confirm uncertain fields, and keep an explicit decision for every record.",
                                action: (0, r.jsxs)(I, {
                                  variant: "outline",
                                  className:
                                    "h-8 border-amber-200 bg-amber-50 px-3 text-amber-800",
                                  children: [eF, " priority samples open"],
                                }),
                              }),
                              (0, r.jsxs)("article", {
                                className:
                                  "overflow-hidden rounded-2xl border border-border bg-white shadow-[0_8px_30px_rgba(24,53,61,0.04)]",
                                children: [
                                  (0, r.jsxs)("div", {
                                    className:
                                      "flex flex-col gap-3 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between",
                                    children: [
                                      (0, r.jsxs)("div", {
                                        className:
                                          "relative min-w-0 flex-1 sm:max-w-sm",
                                        children: [
                                          (0, r.jsx)(C.A, {
                                            className:
                                              "absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground",
                                          }),
                                          (0, r.jsx)(B, {
                                            value: $,
                                            onChange: (e) => Y(e.target.value),
                                            placeholder:
                                              "Search payment, party, or source",
                                            className: "bg-white pl-9",
                                          }),
                                        ],
                                      }),
                                      (0, r.jsxs)("div", {
                                        className:
                                          "flex flex-col gap-2 sm:flex-row",
                                        children: [
                                          (0, r.jsxs)(H, {
                                            value: E,
                                            onValueChange: F,
                                            children: [
                                              (0, r.jsx)(J, {
                                                className:
                                                  "w-full bg-white sm:w-[150px]",
                                                children: (0, r.jsx)(q, {
                                                  placeholder: "Status",
                                                }),
                                              }),
                                              (0, r.jsxs)(K, {
                                                children: [
                                                  (0, r.jsx)(G, {
                                                    value: "all",
                                                    children: "All statuses",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Open",
                                                    children: "Open",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Needs input",
                                                    children: "Needs input",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Approved",
                                                    children: "Approved",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Dismissed",
                                                    children: "Dismissed",
                                                  }),
                                                ],
                                              }),
                                            ],
                                          }),
                                          (0, r.jsxs)(H, {
                                            value: M,
                                            onValueChange: V,
                                            children: [
                                              (0, r.jsx)(J, {
                                                className:
                                                  "w-full bg-white sm:w-[165px]",
                                                children: (0, r.jsx)(q, {
                                                  placeholder: "Address format",
                                                }),
                                              }),
                                              (0, r.jsxs)(K, {
                                                children: [
                                                  (0, r.jsx)(G, {
                                                    value: "all",
                                                    children: "All formats",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Unstructured",
                                                    children: "Unstructured",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Hybrid",
                                                    children: "Hybrid",
                                                  }),
                                                  (0, r.jsx)(G, {
                                                    value: "Structured",
                                                    children: "Structured",
                                                  }),
                                                ],
                                              }),
                                            ],
                                          }),
                                        ],
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)(ed, {
                                    children: [
                                      (0, r.jsx)(el, {
                                        className: "bg-[#f7f9f9]",
                                        children: (0, r.jsxs)(ec, {
                                          children: [
                                            (0, r.jsx)(em, {
                                              className: "pl-4",
                                              children:
                                                "Payment / counterparty",
                                            }),
                                            (0, r.jsx)(em, {
                                              children: "Scheme",
                                            }),
                                            (0, r.jsx)(em, {
                                              children: "Source",
                                            }),
                                            (0, r.jsx)(em, {
                                              children: "Format",
                                            }),
                                            (0, r.jsx)(em, {
                                              children: "Confidence",
                                            }),
                                            (0, r.jsx)(em, {
                                              children: "Status",
                                            }),
                                            (0, r.jsx)(em, {
                                              className: "pr-4 text-right",
                                              children: "Action",
                                            }),
                                          ],
                                        }),
                                      }),
                                      (0, r.jsx)(eo, {
                                        children: eM.map((e) =>
                                          (0, r.jsxs)(
                                            ec,
                                            {
                                              className: "cursor-pointer",
                                              onClick: () => e$(e),
                                              children: [
                                                (0, r.jsxs)(ex, {
                                                  className:
                                                    "min-w-[245px] pl-4",
                                                  children: [
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "block font-medium text-foreground",
                                                      children: e.counterparty,
                                                    }),
                                                    (0, r.jsxs)("span", {
                                                      className:
                                                        "mt-1 block text-xs text-muted-foreground",
                                                      children: [
                                                        e.id,
                                                        " \xb7 ",
                                                        eC(
                                                          e.amount,
                                                          e.currency,
                                                        ),
                                                      ],
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsx)(ex, {
                                                  children: e.scheme,
                                                }),
                                                (0, r.jsx)(ex, {
                                                  children: e.source,
                                                }),
                                                (0, r.jsx)(ex, {
                                                  children: (0, r.jsx)(I, {
                                                    variant: "outline",
                                                    className: ew[e.format],
                                                    children: e.format,
                                                  }),
                                                }),
                                                (0, r.jsx)(ex, {
                                                  children: (0, r.jsxs)(
                                                    "span",
                                                    {
                                                      className:
                                                        "font-semibold",
                                                      children: [
                                                        e.confidence,
                                                        "%",
                                                      ],
                                                    },
                                                  ),
                                                }),
                                                (0, r.jsx)(ex, {
                                                  children: (0, r.jsx)(I, {
                                                    variant: "outline",
                                                    className: ey[e.status],
                                                    children: e.status,
                                                  }),
                                                }),
                                                (0, r.jsx)(ex, {
                                                  className: "pr-4 text-right",
                                                  children: (0, r.jsxs)(U, {
                                                    variant: "ghost",
                                                    size: "sm",
                                                    onClick: (t) => {
                                                      (t.stopPropagation(),
                                                        e$(e));
                                                    },
                                                    children: [
                                                      "Review ",
                                                      (0, r.jsx)(v.A, {}),
                                                    ],
                                                  }),
                                                }),
                                              ],
                                            },
                                            e.id,
                                          ),
                                        ),
                                      }),
                                    ],
                                  }),
                                  0 === eM.length &&
                                    (0, r.jsxs)("div", {
                                      className: "px-6 py-14 text-center",
                                      children: [
                                        (0, r.jsx)(C.A, {
                                          className:
                                            "mx-auto size-7 text-muted-foreground",
                                        }),
                                        (0, r.jsx)("p", {
                                          className: "mt-3 font-medium",
                                          children:
                                            "No records match these filters",
                                        }),
                                        (0, r.jsx)(U, {
                                          variant: "link",
                                          className: "mt-1",
                                          onClick: () => {
                                            (Y(""), F("all"), V("all"));
                                          },
                                          children: "Clear filters",
                                        }),
                                      ],
                                    }),
                                ],
                              }),
                            ],
                          }),
                          (0, r.jsxs)(eg, {
                            value: "cutover",
                            className: "soft-enter space-y-6",
                            children: [
                              (0, r.jsx)(eS, {
                                eyebrow: "Forward-looking control",
                                title: "Cutover simulation lab",
                                description:
                                  "Estimate which payment population would pass or fail if the current EPC planning date applied to today’s data.",
                              }),
                              (0, r.jsxs)("div", {
                                className:
                                  "grid gap-5 xl:grid-cols-[0.72fr_1.28fr]",
                                children: [
                                  (0, r.jsxs)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-white p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: [
                                      (0, r.jsx)("h2", {
                                        className: "text-lg font-semibold",
                                        children: "Scenario setup",
                                      }),
                                      (0, r.jsxs)("div", {
                                        className: "mt-6 space-y-5",
                                        children: [
                                          (0, r.jsxs)("label", {
                                            className: "block",
                                            children: [
                                              (0, r.jsx)("span", {
                                                className:
                                                  "mb-2 block text-sm font-medium",
                                                children: "Payment scheme",
                                              }),
                                              (0, r.jsxs)(H, {
                                                value: et,
                                                onValueChange: eu,
                                                children: [
                                                  (0, r.jsx)(J, {
                                                    className:
                                                      "w-full bg-white",
                                                    children: (0, r.jsx)(q, {}),
                                                  }),
                                                  (0, r.jsxs)(K, {
                                                    children: [
                                                      (0, r.jsx)(G, {
                                                        value: "all",
                                                        children:
                                                          "All EPC schemes",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "SCT",
                                                        children: "SCT",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "SCT Inst",
                                                        children: "SCT Inst",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "SDD Core",
                                                        children: "SDD Core",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "SDD B2B",
                                                        children: "SDD B2B",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "OCT Inst",
                                                        children: "OCT Inst",
                                                      }),
                                                    ],
                                                  }),
                                                ],
                                              }),
                                            ],
                                          }),
                                          (0, r.jsxs)("label", {
                                            className: "block",
                                            children: [
                                              (0, r.jsx)("span", {
                                                className:
                                                  "mb-2 block text-sm font-medium",
                                                children: "Execution date",
                                              }),
                                              (0, r.jsx)(B, {
                                                type: "date",
                                                defaultValue: "2026-11-16",
                                                min: "2026-09-02",
                                              }),
                                            ],
                                          }),
                                          (0, r.jsxs)("label", {
                                            className: "block",
                                            children: [
                                              (0, r.jsx)("span", {
                                                className:
                                                  "mb-2 block text-sm font-medium",
                                                children: "Data state",
                                              }),
                                              (0, r.jsxs)(H, {
                                                value: eh,
                                                onValueChange: ez,
                                                children: [
                                                  (0, r.jsx)(J, {
                                                    className:
                                                      "w-full bg-white",
                                                    children: (0, r.jsx)(q, {}),
                                                  }),
                                                  (0, r.jsxs)(K, {
                                                    children: [
                                                      (0, r.jsx)(G, {
                                                        value: "current",
                                                        children:
                                                          "Current data quality",
                                                      }),
                                                      (0, r.jsx)(G, {
                                                        value: "remediated",
                                                        children:
                                                          "After approved remediation",
                                                      }),
                                                    ],
                                                  }),
                                                ],
                                              }),
                                            ],
                                          }),
                                          (0, r.jsxs)(U, {
                                            className: "w-full",
                                            size: "lg",
                                            onClick: function () {
                                              let e = {
                                                  all: 12840,
                                                  SCT: 6260,
                                                  "SCT Inst": 3540,
                                                  "SDD Core": 1870,
                                                  "SDD B2B": 760,
                                                  "OCT Inst": 410,
                                                },
                                                t = e[et] || e.all,
                                                s = Math.max(
                                                  7,
                                                  Math.round(
                                                    t *
                                                      ("OCT Inst" === et
                                                        ? 0.09
                                                        : et.startsWith("SDD")
                                                          ? 0.118
                                                          : 0.142) *
                                                      ("remediated" === eh
                                                        ? 0.23
                                                        : 1),
                                                  ),
                                                );
                                              eP({
                                                accepted: t - s,
                                                rejected: s,
                                                rate:
                                                  Math.round((s / t) * 1e3) /
                                                  10,
                                              });
                                            },
                                            children: [
                                              (0, r.jsx)(c.A, {}),
                                              " Run simulation",
                                            ],
                                          }),
                                        ],
                                      }),
                                      (0, r.jsxs)("div", {
                                        className:
                                          "mt-6 rounded-xl bg-muted p-4 text-xs leading-5 text-muted-foreground",
                                        children: [
                                          (0, r.jsx)(k.A, {
                                            className: "mr-1 inline size-3.5",
                                          }),
                                          " This is a deterministic demo model, not a substitute for scheme certification or bank-specific implementation guidelines.",
                                        ],
                                      }),
                                    ],
                                  }),
                                  (0, r.jsx)("article", {
                                    className:
                                      "rounded-2xl border border-border bg-white p-5 shadow-[0_8px_30px_rgba(24,53,61,0.04)] sm:p-6",
                                    children: eT
                                      ? (0, r.jsxs)("div", {
                                          children: [
                                            (0, r.jsxs)("div", {
                                              className:
                                                "flex flex-col justify-between gap-4 sm:flex-row sm:items-start",
                                              children: [
                                                (0, r.jsxs)("div", {
                                                  children: [
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "text-xs font-semibold uppercase tracking-[0.16em] text-primary",
                                                      children:
                                                        "Simulation result",
                                                    }),
                                                    (0, r.jsxs)("h2", {
                                                      className:
                                                        "mt-2 text-2xl font-semibold tracking-[-0.03em]",
                                                      children: [
                                                        eT.rejected.toLocaleString(),
                                                        " payments projected to reject",
                                                      ],
                                                    }),
                                                    (0, r.jsxs)("p", {
                                                      className:
                                                        "mt-2 text-sm text-muted-foreground",
                                                      children: [
                                                        "all" === et
                                                          ? "All EPC schemes"
                                                          : et,
                                                        " \xb7 ",
                                                        "current" === eh
                                                          ? "current data quality"
                                                          : "approved remediation applied",
                                                      ],
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsxs)(I, {
                                                  variant: "outline",
                                                  className:
                                                    eT.rate > 5
                                                      ? "border-red-200 bg-red-50 text-red-700"
                                                      : "border-amber-200 bg-amber-50 text-amber-800",
                                                  children: [
                                                    eT.rate,
                                                    "% rejection rate",
                                                  ],
                                                }),
                                              ],
                                            }),
                                            (0, r.jsxs)("div", {
                                              className:
                                                "mt-8 grid gap-4 sm:grid-cols-2",
                                              children: [
                                                (0, r.jsxs)("div", {
                                                  className:
                                                    "rounded-2xl border border-emerald-200 bg-emerald-50 p-5",
                                                  children: [
                                                    (0, r.jsx)(A.A, {
                                                      className:
                                                        "size-5 text-emerald-700",
                                                    }),
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "mt-4 text-3xl font-semibold text-emerald-800",
                                                      children:
                                                        eT.accepted.toLocaleString(),
                                                    }),
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "mt-1 text-sm text-emerald-700",
                                                      children:
                                                        "Projected accepted",
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsxs)("div", {
                                                  className:
                                                    "rounded-2xl border border-red-200 bg-red-50 p-5",
                                                  children: [
                                                    (0, r.jsx)(S.A, {
                                                      className:
                                                        "size-5 text-red-700",
                                                    }),
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "mt-4 text-3xl font-semibold text-red-800",
                                                      children:
                                                        eT.rejected.toLocaleString(),
                                                    }),
                                                    (0, r.jsx)("p", {
                                                      className:
                                                        "mt-1 text-sm text-red-700",
                                                      children:
                                                        "Projected rejected",
                                                    }),
                                                  ],
                                                }),
                                              ],
                                            }),
                                            (0, r.jsxs)("div", {
                                              className: "mt-7",
                                              children: [
                                                (0, r.jsxs)("div", {
                                                  className:
                                                    "mb-2 flex justify-between text-sm",
                                                  children: [
                                                    (0, r.jsx)("span", {
                                                      className: "font-medium",
                                                      children:
                                                        "Projected processing outcome",
                                                    }),
                                                    (0, r.jsx)("span", {
                                                      className:
                                                        "text-muted-foreground",
                                                      children:
                                                        "100% batch volume",
                                                    }),
                                                  ],
                                                }),
                                                (0, r.jsxs)("div", {
                                                  className:
                                                    "flex h-5 overflow-hidden rounded-full bg-muted",
                                                  children: [
                                                    (0, r.jsx)("div", {
                                                      className: "bg-[#168b7f]",
                                                      style: {
                                                        width: `${100 - eT.rate}%`,
                                                      },
                                                    }),
                                                    (0, r.jsx)("div", {
                                                      className: "bg-[#d45e4b]",
                                                      style: {
                                                        width: `${eT.rate}%`,
                                                      },
                                                    }),
                                                  ],
                                                }),
                                              ],
                                            }),
                                            (0, r.jsxs)("div", {
                                              className:
                                                "mt-7 rounded-2xl border border-[#b9ded8] bg-[#effaf8] p-5",
                                              children: [
                                                (0, r.jsx)("h3", {
                                                  className:
                                                    "font-semibold text-[#164a46]",
                                                  children:
                                                    "Control recommendation",
                                                }),
                                                (0, r.jsx)("p", {
                                                  className:
                                                    "mt-2 text-sm leading-6 text-[#436d69]",
                                                  children:
                                                    "current" === eh
                                                      ? "Complete high-confidence review items first, then rerun this scenario with approved remediation applied."
                                                      : "Residual risk is concentrated in low-confidence records. Request source-owner confirmation rather than auto-correcting them.",
                                                }),
                                                (0, r.jsxs)(U, {
                                                  variant: "outline",
                                                  className:
                                                    "mt-4 border-[#9acfc7] bg-white text-[#075c57]",
                                                  onClick: () =>
                                                    t("remediation"),
                                                  children: [
                                                    "Open remediation ",
                                                    (0, r.jsx)(f.A, {}),
                                                  ],
                                                }),
                                              ],
                                            }),
                                          ],
                                        })
                                      : (0, r.jsxs)("div", {
                                          className:
                                            "flex min-h-[520px] flex-col items-center justify-center text-center",
                                          children: [
                                            (0, r.jsx)("div", {
                                              className:
                                                "flex size-16 items-center justify-center rounded-2xl bg-secondary text-primary",
                                              children: (0, r.jsx)(z.A, {
                                                className: "size-7",
                                              }),
                                            }),
                                            (0, r.jsx)("h2", {
                                              className:
                                                "mt-5 text-xl font-semibold",
                                              children:
                                                "Configure and run a scenario",
                                            }),
                                            (0, r.jsx)("p", {
                                              className:
                                                "mt-2 max-w-md text-sm leading-6 text-muted-foreground",
                                              children:
                                                "The lab compares expected acceptance and rejection volumes using the selected scheme and remediation state.",
                                            }),
                                          ],
                                        }),
                                  }),
                                ],
                              }),
                              (0, r.jsx)("article", {
                                className:
                                  "rounded-2xl border border-border bg-white p-5 sm:p-6",
                                children: (0, r.jsxs)("div", {
                                  className: "grid gap-6 lg:grid-cols-3",
                                  children: [
                                    (0, r.jsxs)("div", {
                                      children: [
                                        (0, r.jsx)("p", {
                                          className:
                                            "text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground",
                                          children: "Test condition 01",
                                        }),
                                        (0, r.jsx)("h3", {
                                          className: "mt-2 font-semibold",
                                          children: "Future-dated payments",
                                        }),
                                        (0, r.jsx)("p", {
                                          className:
                                            "mt-2 text-sm leading-6 text-muted-foreground",
                                          children:
                                            "Use execution or settlement dates after the current planning date, including instructions submitted earlier.",
                                        }),
                                      ],
                                    }),
                                    (0, r.jsxs)("div", {
                                      children: [
                                        (0, r.jsx)("p", {
                                          className:
                                            "text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground",
                                          children: "Test condition 02",
                                        }),
                                        (0, r.jsx)("h3", {
                                          className: "mt-2 font-semibold",
                                          children: "All affected schemes",
                                        }),
                                        (0, r.jsx)("p", {
                                          className:
                                            "mt-2 text-sm leading-6 text-muted-foreground",
                                          children:
                                            "Test SCT, SCT Inst, SDD Core, SDD B2B and OCT Inst flows independently.",
                                        }),
                                      ],
                                    }),
                                    (0, r.jsxs)("div", {
                                      children: [
                                        (0, r.jsx)("p", {
                                          className:
                                            "text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground",
                                          children: "Test condition 03",
                                        }),
                                        (0, r.jsx)("h3", {
                                          className: "mt-2 font-semibold",
                                          children: "Source-owned confirmation",
                                        }),
                                        (0, r.jsx)("p", {
                                          className:
                                            "mt-2 text-sm leading-6 text-muted-foreground",
                                          children:
                                            "Keep low-confidence address changes outside automatic remediation.",
                                        }),
                                      ],
                                    }),
                                  ],
                                }),
                              }),
                            ],
                          }),
                        ],
                      }),
                    }),
                  ],
                }),
              ],
            }),
            (0, r.jsx)(Q, {
              open: !!_,
              onOpenChange: (e) => {
                e || (L(null), O(null));
              },
              children: (0, r.jsx)(es, {
                className: "w-full overflow-y-auto p-0 sm:max-w-xl",
                children:
                  _ &&
                  D &&
                  (0, r.jsxs)(r.Fragment, {
                    children: [
                      (0, r.jsxs)(er, {
                        className: "border-b border-border p-5 pr-12 sm:p-6",
                        children: [
                          (0, r.jsxs)("div", {
                            className: "flex items-center gap-2",
                            children: [
                              (0, r.jsx)(I, {
                                variant: "outline",
                                className: ew[_.format],
                                children: _.format,
                              }),
                              (0, r.jsx)(I, {
                                variant: "outline",
                                className: ey[_.status],
                                children: _.status,
                              }),
                            ],
                          }),
                          (0, r.jsxs)(en, {
                            className: "mt-2 text-xl",
                            children: ["Review ", _.id],
                          }),
                          (0, r.jsxs)(ei, {
                            children: [
                              _.counterparty,
                              " \xb7 ",
                              _.scheme,
                              " \xb7 ",
                              eC(_.amount, _.currency),
                            ],
                          }),
                        ],
                      }),
                      (0, r.jsxs)("div", {
                        className: "space-y-6 p-5 sm:p-6",
                        children: [
                          (0, r.jsxs)("section", {
                            children: [
                              (0, r.jsxs)("div", {
                                className: "flex items-center justify-between",
                                children: [
                                  (0, r.jsx)("h3", {
                                    className: "text-sm font-semibold",
                                    children: "Original address",
                                  }),
                                  (0, r.jsx)("span", {
                                    className: "text-xs text-muted-foreground",
                                    children: _.source,
                                  }),
                                ],
                              }),
                              (0, r.jsx)("div", {
                                className:
                                  "mt-3 rounded-xl border border-red-200 bg-red-50 p-4 font-mono text-sm leading-6 text-red-900",
                                children: _.original,
                              }),
                              _.issues.length > 0 &&
                                (0, r.jsx)("div", {
                                  className: "mt-3 space-y-2",
                                  children: _.issues.map((e) =>
                                    (0, r.jsxs)(
                                      "div",
                                      {
                                        className:
                                          "flex items-center gap-2 text-sm text-red-700",
                                        children: [
                                          (0, r.jsx)(g.A, {
                                            className: "size-4",
                                          }),
                                          " ",
                                          e,
                                        ],
                                      },
                                      e,
                                    ),
                                  ),
                                }),
                            ],
                          }),
                          (0, r.jsxs)("section", {
                            className: "border-t border-border pt-6",
                            children: [
                              (0, r.jsxs)("div", {
                                className:
                                  "flex items-center justify-between gap-3",
                                children: [
                                  (0, r.jsxs)("div", {
                                    children: [
                                      (0, r.jsx)("h3", {
                                        className: "text-sm font-semibold",
                                        children: "Proposed structured address",
                                      }),
                                      (0, r.jsx)("p", {
                                        className:
                                          "mt-1 text-xs text-muted-foreground",
                                        children:
                                          "Verify against authoritative customer or counterparty data.",
                                      }),
                                    ],
                                  }),
                                  (0, r.jsxs)(I, {
                                    variant: "outline",
                                    className:
                                      "border-primary/20 bg-secondary text-primary",
                                    children: [
                                      (0, r.jsx)(w.A, {}),
                                      " ",
                                      _.confidence,
                                      "% confidence",
                                    ],
                                  }),
                                ],
                              }),
                              (0, r.jsx)("div", {
                                className: "mt-5 grid gap-4 sm:grid-cols-2",
                                children: [
                                  ["Street name", "street"],
                                  ["Building number", "buildingNumber"],
                                  ["Post code", "postCode"],
                                  ["Town name", "town"],
                                  ["Country (ISO 2)", "country"],
                                  ["Optional address line", "addressLine"],
                                ].map(([e, t]) =>
                                  (0, r.jsxs)(
                                    "label",
                                    {
                                      className: "block",
                                      children: [
                                        (0, r.jsx)("span", {
                                          className:
                                            "mb-2 block text-xs font-medium text-muted-foreground",
                                          children: e,
                                        }),
                                        (0, r.jsx)(B, {
                                          value: D[t],
                                          onChange: (e) =>
                                            O((s) =>
                                              s
                                                ? { ...s, [t]: e.target.value }
                                                : s,
                                            ),
                                          className: "bg-white",
                                        }),
                                      ],
                                    },
                                    t,
                                  ),
                                ),
                              }),
                            ],
                          }),
                          (0, r.jsx)("section", {
                            className:
                              "rounded-xl border border-[#b9ded8] bg-[#effaf8] p-4",
                            children: (0, r.jsxs)("div", {
                              className: "flex items-start gap-3",
                              children: [
                                (0, r.jsx)(b.A, {
                                  className:
                                    "mt-0.5 size-5 shrink-0 text-primary",
                                }),
                                (0, r.jsxs)("div", {
                                  children: [
                                    (0, r.jsx)("h3", {
                                      className:
                                        "text-sm font-semibold text-[#164a46]",
                                      children: "Human approval required",
                                    }),
                                    (0, r.jsx)("p", {
                                      className:
                                        "mt-1 text-xs leading-5 text-[#436d69]",
                                      children:
                                        "The proposal is never applied silently. Approving records the reviewed values in this browser demo.",
                                    }),
                                  ],
                                }),
                              ],
                            }),
                          }),
                        ],
                      }),
                      (0, r.jsxs)(ea, {
                        className:
                          "sticky bottom-0 border-t border-border bg-white p-5 sm:p-6",
                        children: [
                          (0, r.jsxs)("div", {
                            className: "grid gap-2 sm:grid-cols-2",
                            children: [
                              (0, r.jsxs)(U, {
                                variant: "outline",
                                onClick: () => eH("Needs input"),
                                children: [
                                  (0, r.jsx)(i.A, {}),
                                  " Request owner input",
                                ],
                              }),
                              (0, r.jsxs)(U, {
                                onClick: () => eH("Approved"),
                                children: [
                                  (0, r.jsx)(T.A, {}),
                                  " Approve correction",
                                ],
                              }),
                            ],
                          }),
                          (0, r.jsx)(U, {
                            variant: "ghost",
                            size: "sm",
                            className: "text-muted-foreground",
                            onClick: () => eH("Dismissed"),
                            children: "Dismiss from queue",
                          }),
                        ],
                      }),
                    ],
                  }),
              }),
            }),
            (0, r.jsx)(Q, {
              open: eL,
              onOpenChange: eD,
              children: (0, r.jsxs)(es, {
                className: "w-full overflow-y-auto p-0 sm:max-w-lg",
                children: [
                  (0, r.jsxs)(er, {
                    className: "border-b border-border p-5 pr-12 sm:p-6",
                    children: [
                      (0, r.jsx)(I, {
                        variant: "outline",
                        className:
                          "mb-2 border-primary/20 bg-secondary text-primary",
                        children: "MVP guide",
                      }),
                      (0, r.jsx)(en, {
                        className: "text-xl",
                        children: "Using PayData Control",
                      }),
                      (0, r.jsx)(ei, {
                        children:
                          "Four short workflows cover readiness assessment through cutover simulation.",
                      }),
                    ],
                  }),
                  (0, r.jsxs)("div", {
                    className: "space-y-6 p-5 sm:p-6",
                    children: [
                      [
                        [
                          d.A,
                          "1. Review portfolio readiness",
                          "Start on Overview to identify the weakest source systems, highest-volume issues, and the current EPC planning status.",
                        ],
                        [
                          l.A,
                          "2. Analyze a payment file",
                          "Open File analyzer, upload XML or CSV, or use the sample. The browser checks postal-address structure without sending the file anywhere.",
                        ],
                        [
                          o.A,
                          "3. Verify remediation proposals",
                          "Open a queue record, compare the original address with the proposed fields, edit if needed, then approve or request source-owner input.",
                        ],
                        [
                          c.A,
                          "4. Simulate cutover exposure",
                          "Choose a scheme and data state in Cutover lab to compare projected accepted and rejected payment volumes.",
                        ],
                      ].map(([e, t, s]) =>
                        (0, r.jsxs)(
                          "section",
                          {
                            className: "flex gap-3",
                            children: [
                              (0, r.jsx)("span", {
                                className:
                                  "flex size-10 shrink-0 items-center justify-center rounded-xl bg-secondary text-primary",
                                children: (0, r.jsx)(e, {
                                  className: "size-5",
                                }),
                              }),
                              (0, r.jsxs)("div", {
                                children: [
                                  (0, r.jsx)("h3", {
                                    className: "font-semibold",
                                    children: String(t),
                                  }),
                                  (0, r.jsx)("p", {
                                    className:
                                      "mt-1 text-sm leading-6 text-muted-foreground",
                                    children: String(s),
                                  }),
                                ],
                              }),
                            ],
                          },
                          String(t),
                        ),
                      ),
                      (0, r.jsxs)("div", {
                        className:
                          "rounded-xl border border-border bg-muted/60 p-4",
                        children: [
                          (0, r.jsx)("h3", {
                            className: "text-sm font-semibold",
                            children: "Local data behavior",
                          }),
                          (0, r.jsx)("p", {
                            className:
                              "mt-2 text-xs leading-5 text-muted-foreground",
                            children:
                              "Review decisions and the latest scan summary persist in this browser. Clearing site data removes them. The imported file itself is not retained.",
                          }),
                        ],
                      }),
                    ],
                  }),
                  (0, r.jsxs)(ea, {
                    className: "border-t border-border bg-white p-5 sm:p-6",
                    children: [
                      (0, r.jsxs)(U, {
                        variant: "outline",
                        onClick: function () {
                          (R(ej),
                            X(null),
                            eP(null),
                            localStorage.removeItem("paydata-records-v1"),
                            localStorage.removeItem("paydata-scan-v1"),
                            eV("Demo data restored"));
                        },
                        children: [(0, r.jsx)(P.A, {}), " Reset demo data"],
                      }),
                      (0, r.jsx)(U, {
                        onClick: () => eD(!1),
                        children: "Continue to workspace",
                      }),
                    ],
                  }),
                ],
              }),
            }),
            eR &&
              (0, r.jsxs)("div", {
                className:
                  "fixed bottom-5 left-1/2 z-[70] flex -translate-x-1/2 items-center gap-2 rounded-full bg-[#102832] px-4 py-2.5 text-sm font-medium text-white shadow-xl",
                role: "status",
                children: [
                  (0, r.jsx)(A.A, { className: "size-4 text-[#76d7cc]" }),
                  " ",
                  eR,
                ],
              }),
          ],
        });
      }
    },
  },
  (e) => {
    (e.O(0, [768, 182, 4, 358], () => e((e.s = 2560))), (_N_E = e.O()));
  },
]);

from __future__ import annotations

from typing import Any, Dict, List


def ensure_ui_when_requested(
    plan: Dict[str, Any],
    user_prompt: str,
    shortlisted_components: List[Dict[str, Any]],
    shortlisted_tools: List[Dict[str, Any]],
) -> Dict[str, Any]:
    if not isinstance(plan, dict):
        return plan

    prompt = (user_prompt or "").lower()
    steps = plan.get("steps", [])
    if not isinstance(steps, list):
        steps = []
        plan["steps"] = steps

    available_components = {
        str(c.get("name", "")).strip()
        for c in shortlisted_components
        if isinstance(c, dict)
    }
    available_tools = {
        str(t.get("name", "")).strip()
        for t in shortlisted_tools
        if isinstance(t, dict)
    }

    existing_ui = plan.get("uiIntent")
    if isinstance(existing_ui, dict) and existing_ui.get("component_tree"):
        safety = plan.get("safety")
        if not isinstance(safety, dict):
            safety = {}
        safety["needAssistantAnswer"] = False if steps else True
        safety.setdefault("reason", "UI intent provided by planner.")
        plan["safety"] = safety
        return plan

    ui_requested = any(
        k in prompt
        for k in [
            "ui",
            "component",
            "react",
            "app.tsx",
            "landing page",
            "card",
            "chart",
            "piechart",
            "bar chart",
            "table",
            "tabel",
            "dashboard",
            "detalii",
            "grafic",
            "grafice",
            "diagram",
            "diagrama",
        ]
    )

    domain_shop = any(k in prompt for k in ["product", "produs", "buy", "cump", "cart", "shop", "macbook", "laptop"])
    domain_finance = any(k in prompt for k in ["expense", "cheltu", "income", "venit", "budget", "finance", "transaction", "analytics"])
    domain_code = any(k in prompt for k in ["code", "cod", "typescript", "javascript", "python", "app.tsx", "componenta"])
    domain_visualization = any(
        k in prompt
        for k in [
            "grafic",
            "grafice",
            "chart",
            "piechart",
            "bar chart",
            "line chart",
            "toate formele grafice",
            "all charts",
        ]
    )
    wants_input = any(k in prompt for k in ["input", "custom", "customiz", "form", "formular"])

    first_tool = steps[0]["tool"] if steps and isinstance(steps[0], dict) and "tool" in steps[0] else ""

    def has_component(name: str) -> bool:
        return name in available_components

    def comp(name: str, props: Dict[str, Any]) -> Dict[str, Any]:
        return {"type": "component", "name": name, "props": props, "children": []}

    # If shortlist is too narrow, keep shop flows usable via deterministic fallbacks.
    if domain_shop:
        if "shop.searchProducts" not in available_tools and "shop.listProducts" not in available_tools:
            available_tools.add("shop.listProducts")
        available_tools.add("shop.searchProducts")
        available_tools.add("shop.listCategories")

        # Ensure core shop UI blocks are available even when component ranking misses them.
        available_components.update(
            {
                "pageTitle",
                "searchBar",
                "categoryChips",
                "sortDropdown",
                "productCarousel",
                "pagination",
                "dataTable",
                "actionPanel",
            }
        )

    if not steps and domain_shop:
        if "shop.searchProducts" in available_tools:
            steps.append({"id": "step_1", "tool": "shop.searchProducts", "args": {"q": user_prompt, "limit": 12, "skip": 0}, "cache_ttl_seconds": None})
        elif "shop.listProducts" in available_tools:
            steps.append({"id": "step_1", "tool": "shop.listProducts", "args": {"limit": 12, "skip": 0}, "cache_ttl_seconds": None})
        if "shop.listCategories" in available_tools:
            steps.append({"id": "step_2", "tool": "shop.listCategories", "args": {}, "cache_ttl_seconds": None})

    if not steps and domain_finance:
        if "finance.getSummary" in available_tools:
            steps.append({"id": "step_1", "tool": "finance.getSummary", "args": {}, "cache_ttl_seconds": None})
        if "finance.getExpenseAnalytics" in available_tools:
            steps.append({"id": "step_2", "tool": "finance.getExpenseAnalytics", "args": {"period": "last30days"}, "cache_ttl_seconds": None})
        if "finance.getTransactions" in available_tools:
            steps.append({"id": "step_3", "tool": "finance.getTransactions", "args": {"skip": 0, "limit": 20}, "cache_ttl_seconds": None})

    root_children: List[Dict[str, Any]] = []
    bindings: List[Dict[str, Any]] = []
    subscriptions: List[Dict[str, Any]] = []

    if has_component("pageTitle"):
        root_children.append(comp("pageTitle", {"text": "MIBO Assistant", "subtitle": "Generative UI response"}))

    if domain_shop and has_component("searchBar"):
        root_children.append(comp("searchBar", {"placeholder": "Search products...", "actionType": "shop.search"}))

    if domain_shop and has_component("categoryChips"):
        root_children.append(comp("categoryChips", {"actionType": "shop.select_category"}))
        if any((s.get("tool") == "shop.listCategories") for s in steps if isinstance(s, dict)):
            idx = len(root_children) - 1
            bindings.append({"componentPath": f"/root/children/{idx}", "prop": "dataKey", "from": "shop.listCategories"})

    if domain_shop and has_component("sortDropdown"):
        root_children.append(comp("sortDropdown", {"actionType": "shop.sort"}))

    if domain_shop and has_component("productCarousel"):
        source = first_tool or "shop.searchProducts"
        root_children.append(comp("productCarousel", {"title": "Products", "dataKey": source, "actionType": "shop.open_product", "secondaryActionType": "shop.add_to_cart"}))

    if domain_shop and has_component("dataTable"):
        source = first_tool or "shop.searchProducts"
        root_children.append(
            comp(
                "dataTable",
                {
                    "title": "Products Table",
                    "dataKey": source,
                    "rowsKey": "products",
                    "columns": [
                        {"key": "title", "header": "Title"},
                        {"key": "brand", "header": "Brand"},
                        {"key": "price", "header": "Price", "type": "number"},
                        {"key": "rating", "header": "Rating", "type": "number"},
                        {"key": "stock", "header": "Stock", "type": "number"},
                    ],
                    "pageSize": 10,
                },
            )
        )

    if domain_shop and has_component("pagination"):
        source = first_tool or "shop.searchProducts"
        root_children.append(
            comp(
                "pagination",
                {
                    "actionType": "shop.change_page",
                    "totalKey": f"{source}.total",
                    "limitKey": f"{source}.limit",
                    "skipKey": f"{source}.skip",
                },
            )
        )

    if domain_finance and has_component("summaryPanel"):
        root_children.append(comp("summaryPanel", {"title": "Financial Summary", "dataKey": "finance.getSummary.accountBalances"}))

    if domain_finance and has_component("barChart"):
        root_children.append(comp("barChart", {"title": "Expenses by Category", "dataKey": "finance.getExpenseAnalytics.byCategory"}))

    if domain_finance and has_component("lineChart"):
        root_children.append(comp("lineChart", {"title": "Expense Trend", "dataKey": "finance.getExpenseAnalytics.timeSeries"}))

    if domain_finance and has_component("dataTable"):
        root_children.append(
            comp(
                "dataTable",
                {
                    "title": "Transactions",
                    "dataKey": "finance.getTransactions",
                    "columns": [
                        {"key": "date", "header": "Date"},
                        {"key": "category", "header": "Category"},
                        {"key": "amount", "header": "Amount", "type": "number"},
                        {"key": "description", "header": "Description"},
                    ],
                    "rowsKey": "items",
                    "pageSize": 10,
                },
            )
        )

    if domain_code and has_component("markdown"):
        root_children.append(
            comp(
                "markdown",
                {
                    "title": "Generated Code",
                    "content": (
                        "Cererea este orientata pe cod. UI-ul include un editor markdown pentru codul generat "
                        "si poate fi extins cu formulare/actiuni in urmatorii pasi."
                    ),
                    "showCodeHeader": True,
                },
            )
        )

    if domain_visualization:
        demo_gender = [
            {"name": "Femei", "label": "Femei", "value": 80, "color": "#ec4899"},
            {"name": "Barbati", "label": "Barbati", "value": 20, "color": "#3b82f6"},
        ]

        if wants_input and has_component("formCard"):
            root_children.append(
                comp(
                    "formCard",
                    {
                        "title": "Configureaza datele graficului",
                        "subtitle": "Introdu categorii si valori",
                        "actionType": "ui.form.submit",
                        "fields": [
                            {"name": "label1", "label": "Categorie 1", "type": "text", "required": True, "defaultValue": "Categoria A"},
                            {"name": "value1", "label": "Valoare 1", "type": "number", "required": True, "defaultValue": 50},
                            {"name": "label2", "label": "Categorie 2", "type": "text", "required": True, "defaultValue": "Categoria B"},
                            {"name": "value2", "label": "Valoare 2", "type": "number", "required": True, "defaultValue": 20},
                        ],
                    },
                )
            )

        if has_component("summaryPanel"):
            root_children.append(
                comp(
                    "summaryPanel",
                    {
                        "title": "Sumar Date",
                        "items": [
                            {"label": "Femei", "value": 80, "trend": 0, "color": "#ec4899"},
                            {"label": "Barbati", "value": 20, "trend": 0, "color": "#3b82f6"},
                        ],
                        "columns": 2,
                    },
                )
            )

        if has_component("pieChart"):
            root_children.append(
                comp(
                    "pieChart",
                    {
                        "title": "Distributie (Pie)",
                        "data": demo_gender,
                        "formatValue": "{value}%",
                        "showLegend": True,
                    },
                )
            )

        if has_component("barChart"):
            root_children.append(
                comp(
                    "barChart",
                    {
                        "title": "Distributie (Bar)",
                        "data": demo_gender,
                        "showValues": True,
                        "formatValue": "{value}",
                    },
                )
            )

        if has_component("lineChart"):
            root_children.append(
                comp(
                    "lineChart",
                    {
                        "title": "Trend Simulat",
                        "data": [
                            {
                                "name": "Femei",
                                "data": [
                                    {"label": "Set 1", "value": 80},
                                    {"label": "Set 2", "value": 80},
                                ],
                            },
                            {
                                "name": "Barbati",
                                "data": [
                                    {"label": "Set 1", "value": 20},
                                    {"label": "Set 2", "value": 20},
                                ],
                            },
                        ],
                    },
                )
            )

        if has_component("dataTable"):
            root_children.append(
                comp(
                    "dataTable",
                    {
                        "title": "Date Tabelare",
                        "rows": [
                            {"categorie": "Femei", "valoare": 80, "procent": "80%"},
                            {"categorie": "Barbati", "valoare": 20, "procent": "20%"},
                        ],
                        "columns": [
                            {"key": "categorie", "header": "Categorie"},
                            {"key": "valoare", "header": "Valoare", "type": "number"},
                            {"key": "procent", "header": "Procent"},
                        ],
                        "pageSize": 10,
                    },
                )
            )

    if len(root_children) == 1 and root_children[0].get("name") == "pageTitle" and has_component("markdown"):
        root_children.append(
            comp(
                "markdown",
                {
                    "title": "Assistant Response",
                    "content": "Am construit structura UI de baza. Pot extinde cu tabele, grafice, formulare si actiuni in functie de urmatorul prompt.",
                },
            )
        )

    if not root_children:
        if has_component("markdown"):
            root_children.append(
                comp(
                    "markdown",
                    {
                        "title": "Assistant Response",
                        "content": "UI generativ pregatit. Trimite mai multe detalii pentru o compozitie interactiva extinsa.",
                    },
                )
            )
        else:
            return plan

    if has_component("actionPanel"):
        root_children.append(
            comp(
                "actionPanel",
                {
                    "title": "Quick Actions",
                    "actions": [
                        {"label": "Refine", "actionType": "ui.refine", "variant": "secondary"},
                        {"label": "Expand Analysis", "actionType": "ui.expand", "variant": "ghost"},
                    ],
                },
            )
        )

    if domain_shop:
        subscriptions.append(
            {
                "event": "shop.cart_updated",
                "refresh": [
                    {"tool": "shop.getCart", "args": {"cartId": "${tool:shop.addToCart.id}"}, "patchPath": "/data/shop.getCart"}
                ],
            }
        )

    plan["uiIntent"] = {
        "component_tree": {"type": "layout", "name": "column", "props": {"gap": 12}, "children": root_children},
        "bindings": bindings,
        "subscriptions": subscriptions,
    }

    safety = plan.get("safety")
    if not isinstance(safety, dict):
        safety = {}
    safety["needAssistantAnswer"] = False if steps else (False if ui_requested else True)
    safety.setdefault("reason", "UI-first orchestration enforced.")
    plan["safety"] = safety
    return plan

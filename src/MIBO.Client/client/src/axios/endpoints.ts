export const endpoints = {
    auth: {
        login: '/auth/login',
        refresh: '/auth/refresh-token',
    },
    menu: {
        products: '/menu/products',
    },
    category: {
        list: '/categories',
        get: (id: string) => `/category/${id}`,
        create: '/category/create',
        update: (id: string) => `/category/update/${id}`,
        delete: (id: string) => `/category/delete/${id}`,
    },
    label: {
        list: '/labels',
        get: (id: string) => `/label/${id}`,
        create: '/label/create',
        update: (id: string) => `/label/update/${id}`,
        delete: (id: string) => `/label/delete/${id}`,
    },
    topping: {
        list: '/toppings',
        get: (id: string) => `/topping/${id}`,
        create: '/topping/create',
        update: (id: string) => `/topping/update/${id}`,
        delete: (id: string) => `/topping/delete/${id}`,
    },
    product: {
        all: '/products/all',
        allSuggestions: '/product/all-suggestions',
        list: '/products',
        get: (id: string) => `/product/${id}`,
        create: '/product/create',
        update: (id: string) => `/product/update/${id}`,
        updateStockStatus: (id: string) => `/product/update/${id}/stock-status`,
        updateActiveStatus: (id: string) => (`/product/update/${id}/active-status`),
        delete: (id: string) => `/product/delete/${id}`,
    },
    order: {
        list: '/order/all',
        confirm: (id: string, time: number) => `/order/confirm/${id}?estimatedDeliveryTime=${time}`,
        updateStatus: (id: string) => `/order/update-status/${id}`,
        cancel: (id: string) => `/order/cancel/${id}`,
    },
    analytics: {
        kpiCards: (granularity: string) => `/analytics/kpi-cards/${granularity}`,
        timeseries: (metric: string, granularity: string) => `/analytics/timeseries?metric=${metric}&granularity=${granularity}`,
        topProducts: (metric: string, granularity: string) => (`/analytics/top-products?metric=${metric}&granularity=${granularity}`),
    }
}
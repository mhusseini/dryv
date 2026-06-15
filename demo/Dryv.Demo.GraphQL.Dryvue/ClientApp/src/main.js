import { createApp } from 'vue'
import { Dryv, DryvStaticRuleSets } from 'dryvue'
import App from './App.vue'

async function bootstrap() {
    // Fetch validation rules from the server
    const script = document.createElement('script')
    script.src = '/api/validation-rules'
    await new Promise((resolve, reject) => {
        script.onload = resolve
        script.onerror = reject
        document.head.appendChild(script)
    })

    const app = createApp(App)
    app.use(Dryv)
    app.use(DryvStaticRuleSets, window.dryv.v)
    app.mount('#app')
}

bootstrap()

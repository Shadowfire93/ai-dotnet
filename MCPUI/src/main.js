import { createApp } from 'vue'
import App from './App.vue'
import { Quasar } from 'quasar'
import quasarIconSet from 'quasar/icon-set/svg-material-icons'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/src/css/index.sass'

const app = createApp(App)
app.use(Quasar, {
  iconSet: quasarIconSet,
})
app.mount('#app')

import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
})

if (import.meta.env.PROD) {
  api.defaults.baseURL = 'http://localhost:5000/api'
}

export default api

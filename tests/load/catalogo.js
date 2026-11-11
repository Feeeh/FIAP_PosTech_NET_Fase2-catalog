import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const token = __ENV.TOKEN;

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURATION || '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: [`p(95)<${__ENV.P95_MS || 500}`],
  },
};

export default function () {
  const response = token
    ? http.get(`${baseUrl}/api/jogos`, { headers: { Authorization: `Bearer ${token}` } })
    : http.get(`${baseUrl}/saude`);

  check(response, { 'status 200': ({ status }) => status === 200 });
  sleep(1);
}

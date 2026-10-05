import requests
from odoo import models, fields, api, exceptions

class KadroffPosition(models.Model):
    _name = 'kadroff.position'
    _description = 'Должности Kadroff'

    name = fields.Char(string='Наименование должности', required=True)
    api_token = fields.Char(string='X-Api-Token', required=True)
    api_url = fields.Char(string='URL API', default='https://your-app.onrender.com/api/positionsapi/aggregated')
    
    attributes_json = fields.Text(string='Агрегированные данные (JSON)', readonly=True)

    def action_fetch_aggregated_data(self):
        """ Запрос данных из Blazor API """
        self.ensure_one()
        if not self.api_token:
            raise exceptions.UserError("Укажите API Token перед синхронизацией.")

        headers = {
            'X-Api-Token': self.api_token,
            'Content-Type': 'application/json'
        }

        try:
            response = requests.get(self.api_url, headers=headers, timeout=10)
            if response.status_code == 200:
                data = response.json()
                self.name = data.get('position_title', self.name)
                self.attributes_json = str(data.get('attributes'))
            else:
                raise exceptions.UserError(f"Ошибка API ({response.status_code}): {response.text}")
        except Exception as e:
            raise exceptions.UserError(f"Не удалось связаться с сервером Kadroff: {str(e)}")
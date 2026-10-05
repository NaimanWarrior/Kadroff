import requests
from odoo import models, fields, api
from odoo.exceptions import UserError

class KadroffImportedPosition(models.Model):
    _name = 'kadroff.imported.position'
    _description = 'Imported Position Data from Kadroff'
    _rec_name = 'title'

    title = fields.Char(string='Position Title', required=True, readonly=True)
    attribute_ids = fields.One2many('kadroff.position.attribute', 'position_id', string='Attributes', readonly=True)

class KadroffPositionAttribute(models.Model):
    _name = 'kadroff.position.attribute'
    _description = 'Position Attribute Aggregate'

    position_id = fields.Many2one('kadroff.imported.position', ondelete='cascade')
    title = fields.Char(string='Attribute Title', readonly=True)
    attr_type = fields.Selection([('number', 'Number'), ('text', 'Text')], string='Type', readonly=True)
    
    min_value = fields.Float(string='Min Value', readonly=True)
    max_value = fields.Float(string='Max Value', readonly=True)
    avg_value = fields.Float(string='Avg Value', readonly=True)
    most_popular = fields.Char(string='Most Popular Values', readonly=True)

class KadroffImportWizard(models.TransientModel):
    _name = 'kadroff.import.wizard'
    _description = 'Import Data by API Token'

    api_endpoint = fields.Char(
        string='Kadroff API URL', 
        default='http://kadroff_app:8080/api/positions/aggregated', 
        required=True
    )
    api_token = fields.Char(string='API Token', required=True)

    def action_import(self):
        headers = {'X-Api-Token': self.api_token}
        
        try:
            response = requests.get(self.api_endpoint, headers=headers, timeout=10)
            if response.status_code != 200:
                raise UserError(f"API Error ({response.status_code}): {response.text}")
            
            data = response.json()
        except Exception as e:
            raise UserError(f"Could not connect to Kadroff API: {str(e)}")

        position = self.env['kadroff.imported.position'].create({
            'title': data.get('position_title', 'Imported Position')
        })

        for attr in data.get('attributes', []):
            popular_str = ", ".join(attr.get('most_popular') or [])
            self.env['kadroff.position.attribute'].create({
                'position_id': position.id,
                'title': attr.get('title'),
                'attr_type': attr.get('type'),
                'min_value': attr.get('min_value') or 0.0,
                'max_value': attr.get('max_value') or 0.0,
                'avg_value': attr.get('avg_value') or 0.0,
                'most_popular': popular_str
            })

        return {
            'type': 'ir.actions.act_window',
            'res_model': 'kadroff.imported.position',
            'res_id': position.id,
            'view_mode': 'form',
            'target': 'current',
        }
{
    'name': 'Kadroff Positions Import',
    'version': '1.0',
    'category': 'Inventory',
    'summary': 'Read-only viewer for aggregated positions from Kadroff Blazor Portal',
    'depends': ['base'],
    'data': [
        'security/ir.model.access.csv',
        'views/position_views.xml',
    ],
    'installable': True,
    'application': True,
}
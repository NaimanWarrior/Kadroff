<?xml version="1.0" encoding="utf-8"?>
<odoo>
    <!-- Form View (Read-Only) -->
    <record id="view_imported_position_form" model="ir.ui.view">
        <field name="name">kadroff.imported.position.form</field>
        <field name="model">kadroff.imported.position</field>
        <field name="arch" type="xml">
            <form create="false" edit="false">
                <sheet>
                    <group>
                        <field name="title"/>
                    </group>
                    <notebook>
                        <page string="Aggregated Attributes">
                            <field name="attribute_ids">
                                <tree>
                                    <field name="title"/>
                                    <field name="attr_type"/>
                                    <field name="min_value"/>
                                    <field name="max_value"/>
                                    <field name="avg_value"/>
                                    <field name="most_popular"/>
                                </tree>
                            </field>
                        </page>
                    </notebook>
                </sheet>
            </form>
        </field>
    </record>

    <!-- Import Wizard View -->
    <record id="view_import_wizard_form" model="ir.ui.view">
        <field name="name">kadroff.import.wizard.form</field>
        <field name="model">kadroff.import.wizard</field>
        <field name="arch" type="xml">
            <form string="Import Position via API Token">
                <group>
                    <field name="api_endpoint"/>
                    <field name="api_token"/>
                </group>
                <footer>
                    <button name="action_import" string="Import Data" type="object" class="btn-primary"/>
                    <button string="Cancel" class="btn-secondary" special="cancel"/>
                </footer>
            </form>
        </field>
    </record>

    <record id="action_import_wizard" model="ir.actions.act_window">
        <field name="name">Import Data</field>
        <field name="res_model">kadroff.import.wizard</field>
        <field name="view_mode">form</field>
        <field name="target">new</field>
    </record>

    <record id="action_imported_positions" model="ir.actions.act_window">
        <field name="name">Imported Positions</field>
        <field name="res_model">kadroff.imported.position</field>
        <field name="view_mode">tree,form</field>
    </record>

    <!-- Navigation Menu -->
    <menuitem id="menu_kadroff_root" name="Kadroff Integration"/>
    <menuitem id="menu_imported_positions" name="Positions" parent="menu_kadroff_root" action="action_imported_positions"/>
    <menuitem id="menu_import_wizard" name="Import Data" parent="menu_kadroff_root" action="action_import_wizard"/>
</odoo>
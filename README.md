# Acumatica-Employee-Project-TIme
 Acumatica Customization to allow employees to enter time activities from the "Employee Time Activities" (EP307000) screen 

 See the "Business Logic" folder for the customizations to the Business Logic. 
 The actual Time-Clock feature is from Line 118 to 277. 
 Line 118 to 165 are for setting default values for 3 fields.
 The two actions - stop_Timer() and pause_Timer() are from Line 170 to 277

 
 See the "DACs" folder for customizations to the Data Base. 
 The core functionality of the Time-Clock is in PMTimeActivity and EPActivityApprove.  PMTimeActivity is the base DAC. Any changes to EPActivityApprove should be mimicked in PMTimeActivity.
 PMTimeActivityFilter is for customizing the view on the screen.

 There is a UI element needed when publishing this customization. Please see "customization_project_exports" for the .zip file for this customization. If one is building it manually, please add EP307000 to the "Customized Screens". The added DAC fields will be added on the Form: Filter and Grid: Activity sections in whatever UI format the business requires. 
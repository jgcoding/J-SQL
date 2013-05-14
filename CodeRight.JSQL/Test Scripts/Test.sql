﻿-- Examples for queries that exercise different SQL objects implemented by this assembly

-----------------------------------------------------------------------------------------
-- Stored procedure
-----------------------------------------------------------------------------------------
-- exec StoredProcedureName


-----------------------------------------------------------------------------------------
-- User defined function
-----------------------------------------------------------------------------------------
-- select dbo.FunctionName()


-----------------------------------------------------------------------------------------
-- User defined type
-----------------------------------------------------------------------------------------
-- CREATE TABLE test_table (col1 UserType)
--
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 1'))
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 2'))
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 3'))
--
-- select col1::method1() from test_table



-----------------------------------------------------------------------------------------
-- User defined type
-----------------------------------------------------------------------------------------
-- select dbo.AggregateName(Column1) from Table1


DECLARE @jsonTable table(
    Url nvarchar(max) NOT NULL,
    NodeKey nvarchar(50) NOT NULL,
    Node nvarchar(100) NULL,
    ItemKey nvarchar(100) NULL,
    ItemValue nvarchar(max) NULL,
    ItemType varchar(25) NOT NULL,
    Selector nvarchar(max) NULL
    );
 
insert @jsonTable
Select Url, NodeKey, Node, ItemKey, ItemValue, ItemType, null
from Utilities.dbo.RxJsonParse('{"*ID":"fe009994-0761-4688-a35a-02f5bd44d2d1","TruckType/":"48346b70-332a-49c8-8c01-d0170335c986","Weight":51010.00,"TareWeight":0,"Temperature":"See BOL","OverrideLock":false,"PublicLoadID":75553,"StatusInfo":{"LoadStatus/":"f9c5ebf8-793f-47fa-815a-3d82f8286b0c","LoadPlanningStatus/":"7c125e77-137d-4961-a555-8ebc35573afb","Reload":false,"Paid":false,"Submitted":false,"Accepted":false,"Declined":false,"LoadRevised":false,"LoadPlanRevised":false,"CarrierConfirmed":false,"UnloadingChargesVerified":false,"PalletCostVerified":false,"OveragesShortagesVerified":false,"MiscVerified":false,"CapacityUtilization":0.00,"TruckBuySuggested":0.00},"LoadsInHouse":{"BillingNumber":"87841815","InHouseCarrier":false,"InHouseRate":0.10,"InHouseRequest":false,"InHouseBilled":false},"Representatives":[{"*RepAssignmentID":"54dd49fe-8884-48be-94fc-f039b453e233","Responsibility":"Freight Load Set-Up","AssignedRep/":"A680F657-C104-4D98-801B-CFBA7F652925"}],"LoadCustomers":[{"*Customer/":"5358c1c2-109c-4d5e-9c41-416e0a11ef9e","Accounting":{"AccountSummary":{"ChargeCount":2,"TotalCharges":782.14},"AccountCharges":[{"*ChargeID":"bfb3d73c-a14c-483b-bae7-a3e8221c64a3","ChargeDate":"3/11/2011 9:25:10 AM","User/":"a680f657-c104-4d98-801b-cfba7f652925","ChargeDetail/":"b55edba3-737e-48f2-af89-9af545d9ae36","ChargeNumber":2,"ChargeQuantity":1.00,"ChargeRate":550,"ChargeAmount":550.00,"ChargeOperand":"$"},{"*ChargeID":"07b1def6-af60-4dfb-8e8b-cab7b961277a","ChargeDate":"3/11/2011 9:25:10 AM","User/":"a680f657-c104-4d98-801b-cfba7f652925","ChargeDetail/":"8d0605fd-9750-44ce-8d48-df63f343cb7d","ChargeNumber":1,"ChargeQuantity":438.00,"ChargeRate":0.53,"ChargeAmount":232.14,"ChargeOperand":"$"}]},"StatusInfo":{"Completed":false,"Printed":false,"Received":false},"ContactRep":"83e8136a-f1de-4d07-ae1d-3f6f959ce829","ReferenceNumbers":[{"*InstanceID":"5ac99b31-acfe-407a-9b9e-5526e6d98b5e","Ordinal":1,"ReferenceType/":"560B33C5-B52E-4586-AB53-CC8CD98561A4","ReferenceNumber":"87841815"}],"PublicLoadCustomerID":46900}],"LoadCarriers":[{"*Carrier/":"00000000-0000-0000-0000-000000000000","Accounting":{"AccountSummary":{"ChargeCount":3,"TotalCharges":525.60},"AccountCharges":[{"*ChargeID":"a615a96f-b104-4bcf-ac96-03147f5c58f4","ChargeDate":"03/11/2011 09:25:10","User/":"a680f657-c104-4d98-801b-cfba7f652925","ChargeDetail/":"b2382f35-9f35-4b70-8add-b216c8b93b64","ChargeNumber":1,"ChargeQuantity":0.00,"ChargeRate":35,"ChargeAmount":0.00,"ChargeOperand":"$"},{"*ChargeID":"a8690e34-5aab-4832-a3de-3a5767a475f7","ChargeDate":"03/11/2011 09:25:10","User/":"a680f657-c104-4d98-801b-cfba7f652925","ChargeDetail/":"cd52d7df-564b-459d-a345-5bc28f0137ea","ChargeNumber":3,"ChargeQuantity":438.00,"ChargeRate":1.2,"ChargeAmount":525.60,"ChargeOperand":"$"},{"*ChargeID":"cee985f4-00de-4841-94a7-44f6a76d83a2","ChargeDate":"03/11/2011 09:25:10","User/":"a680f657-c104-4d98-801b-cfba7f652925","ChargeDetail/":"8d0605fd-9750-44ce-8d48-df63f343cb7d","ChargeNumber":2,"ChargeQuantity":438.00,"ChargeRate":0,"ChargeAmount":0.00,"ChargeOperand":"$"}]},"PublicLoadCarrierID":0,"Active":false}],"Notes":[{"*NoteID":"3e274eba-2992-4cf0-9274-cc4f55fedac7","Published":"3/11/2011 9:25:10 AM","Author/":"a680f657-c104-4d98-801b-cfba7f652925","NoteText":"***DETENTION MUST BE REPORTED IN REAL TIME***Smithfield must be contacted by the 2.5th hour in order for detention charges to be approved. Call 888-704-2637 in order to report the truck going in to detention. You will be given an auth #. If you are not given that number make sure you get the name of the person you are speaking with. You must be on time for your appointment in order to qualify for detention. If possible follow up with an email to SmithfieldTeamOnsite@mytmc.com. Any questions please see the account rep.","Access":"public","Tags":"General Load Instructions"}],"Accounting":{"CustomerAmount":782.14,"LineHaul":550.00,"CarrierAmount":525.60,"CarrierRate":1.20,"Revenue":256.54,"GrossMargin":32.80,"CommissionRate":0.000,"CommissionAmount":0.00,"Overhead":367.83,"MinimumLoadFee":650.00,"NetProfit":-111.29,"NetMargin":-14.229,"TruckBuySuggested":586.61,"TruckBuy90DayAve":718.22},"LoadTask":{"TaskSummary":{"Origin":"1fbe0367-7064-4f7f-9821-b21d0bf9be0a","Destination":"95c9c357-7f01-4d24-a11c-9f2806076dee","TotalCustomerMileage":438,"TotalCarrierMileage":438,"PickupCount":1,"DeliveryCount":1,"PickupCargoSummary":[{"*SummaryItemID":"f2877837-6b05-459b-bc82-147e723a6ada","Quantity":672.00,"Cargo":"Pieces"}],"DeliveryCargoSummary":[{"*SummaryItemID":"08f4e3a8-7e2d-4f95-bafe-baa5bda284c0","Quantity":672.00,"Cargo":"Pieces"}],"DistanceToFirstDelivery":438,"TimeOfLastCheckCall":"3/25/2011 8:01:25 AM"},"Tasks":[{"*TaskID":"2e575c84-83c0-4c31-9060-42c9689c620c","TaskType":"Drop-Off","TaskNumber":1,"Location/":"95c9c357-7f01-4d24-a11c-9f2806076dee","CustomerDistance":438,"CarrierDistance":438,"ReferenceNumbers":[{"*InstanceID":"9af07ef2-3838-436c-9fbb-857abfea88ac","Ordinal":1,"ReferenceType/":"53B59EDB-AA48-44BE-9A10-91FF3DE67F14","ReferenceNumber":"263/4059001"}],"LoadCargo":[{"*CargoID":"adb5b28a-07ab-4aa5-96d8-48b580e8a6d4","Customer/":"5358c1c2-109c-4d5e-9c41-416e0a11ef9e","CommodityType/":"97460984-AE4F-4B9F-95D9-1928FD938E24","CargoType/":"5F9BD8E3-0162-4510-A273-A48B775AAA2D","UnitQuantity":672.00,"UnitCost":0.00,"Weight":25505.00,"ReferenceNumbers":[{"*InstanceID":"13ab1c54-367c-431c-a413-5e271924decf","Ordinal":2,"ReferenceType/":"7D1A9DC4-5D9A-4478-B225-0956A00297DE","ReferenceNumber":"002630218068"},{"*InstanceID":"5b63909e-665b-473b-abf8-611369567216","Ordinal":2,"ReferenceType/":"7D1A9DC4-5D9A-4478-B225-0956A00297DE","ReferenceNumber":"002630218079"}],"PublicLoadCargoID":233930}],"TaskSchedule":{"AppointmentIsSet":false,"TaskScheduled":"04/05/2011 09:00:00","TravelTimeRequired":0,"TaskDuration":0},"PublicTaskID":215116},{"*TaskID":"e00af7b8-c027-4c1e-aef7-5626e2c26a8a","TaskType":"Pick-Up","TaskNumber":1,"Location/":"1fbe0367-7064-4f7f-9821-b21d0bf9be0a","CustomerDistance":0,"CarrierDistance":0,"LoadCargo":[{"*CargoID":"2da60380-cba8-4e3e-bdb4-5f91b7f57bab","Customer/":"5358c1c2-109c-4d5e-9c41-416e0a11ef9e","CommodityType/":"97460984-AE4F-4B9F-95D9-1928FD938E24","CargoType/":"5F9BD8E3-0162-4510-A273-A48B775AAA2D","UnitQuantity":672.00,"UnitCost":0.00,"Weight":25505.00,"ReferenceNumbers":[{"*InstanceID":"42091eb6-e613-49cf-9b0f-d84d8d52ae15","Ordinal":2,"ReferenceType/":"560B33C5-B52E-4586-AB53-CC8CD98561A4","ReferenceNumber":"87841815"}],"PublicLoadCargoID":233931}],"TaskSchedule":{"AppointmentIsSet":false,"TaskScheduled":"04/04/2011 00:00:00","TravelTimeRequired":0,"TaskDuration":0},"ContactRep":"7567b81c-c63f-4de8-9bff-acfdb57b3c78","PublicTaskID":215117}]},"EventLog":[{"*EventID":"337d644b-f6c6-4e35-8b3c-04f493536da8","EventDateTime":"3/11/2011 9:25:10 AM","EventAction":"Load Ordered","EventResult":"Success","EventTargetObject":"FreightLoad","User/":"a680f657-c104-4d98-801b-cfba7f652925","EventDetail":{"Post":true,"Message":"Load Successfully Ordered."}},{"*EventID":"21293225-bf00-4c71-993a-928b21e8901a","EventDateTime":"3/11/2011 9:25:02 AM","EventAction":"Update","EventResult":"Success","EventTargetObject":"FreightLoad","User/":"a680f657-c104-4d98-801b-cfba7f652925","EventDetail":{"Post":true,"Message":"Load Successfully Updated."}},{"*EventID":"f4b5db89-b893-49cd-9cf3-936a5c980ed9","EventAction":"Submission","EventResult":"Success","EventTargetObject":"FreightLoad","User/":"a680f657-c104-4d98-801b-cfba7f652925","EventDetail":{"Post":true,"Message":"Load Successfully Submitted."}}],"SimpleNumericArray":[3,5,6,7,8,9],"SimpleStringArray":["One","A","Car","Viper"]}')

Select dbo.ToJson(ItemKey, ItemValue) 
from @jsonTable 
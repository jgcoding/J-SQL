use Utilities
declare @jsonTable jsonTable, @nodeid int
DECLARE @json nvarchar(max)
--set @json = '{"AuthorizationKey":"w7wuKlQ2EftwmkhtDokLqbhnMYm2044kVyl/cEfwB23HcKP+DZsQqg==","Freight.dbo.LoadTask":[{"^Type":"Set","*TaskID":"FFEBC74B-AE9A-4948-8664-00006211E08E","TaskNumber":1,"CarrierDistance":1990},{"^Type":"Set","*TaskID":"F44513AB-4626-46A2-933B-00008EE4145E","TaskNumber":2,"TaskRating":1},{"^Type":"Set","*TaskID":"7C656DA6-643F-4601-B497-0000EFD1467A","TaskNumber":3}]}'
--set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","account opened":"/new Date(1369505389647)/","account number":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}}}'
--set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","account opened":"/new Date(1369505389647)/","account number":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}},"colors":["blue","green","red","orange"]}'
--set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","accountOpened":"/new Date(1369505389647)/","accountNumber":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}},"colors":["blue","green","red","orange"],"orders":[{"orderNo":"136950538-1526","orderDate":"/new Date(1369505389950)/","salesRep":"A-1526","orderCenter":"5254","orderDetail":[{"itemNo":"A1221-J13","itemDescription":"50 count .44 magnum hp","itemQuantity":12,"unitCost":18.25,"lineItemTotal":219},{"itemNo":"A1221-J10","itemDescription":"50 count .45 mm hp","itemQuantity":12,"unitCost":21.25,"lineItemTotal":255}]},{"orderNo":"136950530-1526","orderDate":"/new Date(136950530500)/","salesRep":"A-1526","orderCenter":"5254","orderDetail":[{"itemNo":"A1221-J13","itemDescription":"50 count .44 magnum hp","itemQuantity":12,"unitCost":18.25,"lineItemTotal":219},{"itemNo":"A1221-J10","itemDescription":"50 count .45 mm hp","itemQuantity":12,"unitCost":21.25,"lineItemTotal":255}]}]}'
--set @json = '{"*ID":"c0c92193-7652-49e9-b3db-002cf5ea2f5d","LocationID":20559,"LocationName":"ACE GREASE - MILLSTADT, IL","MaxLoads":0,"DeptID":2,"Telephone":"618-332-2296","InActive":false,"LocationInfo":[{"*LocationID":"0a15dae7-aeb5-49c7-9f23-b5cc70af12c1","LocationType":"Company","Address1":"9035 ST RD 163","City":"MILLSTADT","State":"IL","StateAbbr":"IL","StateName":"Illinois","Zip":"62260","CountyName":"Saint Clair","IsPrimary":true,"LocationProfile":{"Longitude":-90.092279,"Latitude":38.463742,"Exact":0,"AreaCodes":["618","730"],"TimeZone":"Central","UTC":-6.0,"ReadOnly":"true"}}],"ContactInfo":[{"*ContactID":"fb913c73-d776-4b96-94e2-235fcff2aeab","ContactGroup":"Company","ContactType":"Telephone","ContactValue":"618-332-2296","IsPrimary":true}],"Representatives":[{"*RepID":"6a6fdbce-16d5-4e73-9942-d4cf6e97009c","ContactName":"MAIN","IsPrimary":true,"ContactInfo":[{"*ContactID":"4b67c6fe-c31f-4a9b-a7aa-185e9070d85e","ContactGroup":"Company","ContactType":"Telephone","ContactValue":"618-332-2296","IsPrimary":true}]}],"EventLog":[{"*EventID":"055289aa-98bd-4af6-a97f-af792c0ece1e","EventDateTime":"05/02/2011 16:26:26","EventAction":"Modified","EventResult":"Success","EventTargetObject":"Location","User/":"32033d3f-476e-4882-819d-3c662880a91e","EventDetail":"\"EventResponse\":\"Freight Location record update.\""}],"PreferredCarriers":[{"*PreferredCarrierID":"6ac0ee7a-6f5d-4525-98f2-763e93a8292c","Carrier/":"2b8a1ca0-8b01-41b5-a2c4-cde9c4df1246","Ordinal":1},{"*PreferredCarrierID":"bb4881e9-43f8-436b-91dd-7d04e6f35825","Carrier/":"413fcd65-45bb-40df-aafc-47336d28d152","Ordinal":2},{"*PreferredCarrierID":"801979cb-1b7c-42ee-9192-78d4876a03c9","Carrier/":"f04f983b-cd5d-410d-8e07-403a5f98cbb1","Ordinal":3},{"*PreferredCarrierID":"5fbd6cbf-ed98-4acb-8956-b0cf48b22960","Carrier/":"bccbb33d-6122-4fd7-8532-6b3b5ae500b8","Ordinal":6},{"*PreferredCarrierID":"8adde493-aabe-4096-8924-3b70eced4e53","Carrier/":"b0386d0c-6336-438f-b9ab-71bff2dd8f90","Ordinal":4},{"*PreferredCarrierID":"262273cc-deb9-4f39-bb79-1e97e12b5ae1","Carrier/":"da6e83dc-c055-416b-8e56-8871ec062f00","Ordinal":5}],"RepAssignments":[{"*RepAssignmentID":"e0084696-6dcd-41b1-832f-78faa80e0698","Responsibility":"Primary Rep","AssignedRep/":"32033D3F-476E-4882-819D-3C662880A91E"},{"*RepAssignmentID":"a5091103-cc4f-42ce-9824-b5d24d30437a","Responsibility":"Location Set-up","AssignedRep/":"32033D3F-476E-4882-819D-3C662880A91E"}]}'
--set @json = '{"menu":{"id":"file","value":"File","popup":{"menuitem":[{"value":"New","onclick":"CreateNewDoc()"},{"value":"Open","onclick":"OpenDoc()"},{"value":"Close","onclick":"CloseDoc()"}]}}}'
set @json = '{"IPAddress":null,"AddressInt":1025,"Zone":1,"Unit":1,"Subzone":0,"Name":"IPC 101 BS","UnitType":"IPC","Longitude":4.302490234375,"Latitude":3.137939453125,"Footprint":100,"Elevation":0,"HasAC":false,"HasUPS901":false,"HasFan":false,"RunMode":0,"Temperature":0,"IsSelected":false,"Relays":[{"UnitName":null,"Port":7,"Name":"","ContactType":null,"DefaultState":null,"IsActivated":false},{"UnitName":null,"Port":8,"Name":"","ContactType":null,"DefaultState":null,"IsActivated":false},{"UnitName":null,"Port":9,"Name":"","ContactType":null,"DefaultState":null,"IsActivated":false}],"StatusItems":[{"UnitName":"IPC 101 BS","Name":"IPC 101 BS IO1","Key":0,"StatusCategory":0,"CounterCategory":0,"Status":0},{"UnitName":"IPC 101 BS","Name":"IPC 101 BS IO2 - All Call","Key":1,"StatusCategory":0,"CounterCategory":0,"Status":0},{"UnitName":"IPC 101 BS","Name":"IPC 101 BS IO3","Key":2,"StatusCategory":0,"CounterCategory":0,"Status":0},{"UnitName":"IPC 101 BS","Name":"PTT","Key":3,"StatusCategory":0,"CounterCategory":2,"Status":0},{"UnitName":"IPC 101 BS","Name":"MAC Address Mismatch","Key":4,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"IP Address Mismatch","Key":5,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"Configuration Mismatch","Key":6,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"Fan","Key":7,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"Audio Amplifier","Key":8,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"Overcurrent","Key":9,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"Battery","Key":10,"StatusCategory":0,"CounterCategory":1,"Status":0},{"UnitName":"IPC 101 BS","Name":"AC","Key":11,"StatusCategory":0,"CounterCategory":1,"Status":0}],"Health":0,"RS232DeviceType":null,"HasClient":false,"IsServer":false,"DisplayType":4,"StatusType":2}'
insert @jsonTable
SELECT * from Utilities.dbo.ToJsonTable(@json)

--SELECT * from @jsonTable
--declare @jsonOut Table(
--	[parentId] int, 
--	[objectId] int, 
--	token varchar(100), 
--	json nvarchar(max)	
--)
--while 1=1
--begin
--	select @nodeid = nodeid from dbo.GetNode(@jsonTable,0);	
--	if @nodeid is null
--	break;
--	select nodeId, json from dbo.GetNode(@jsonTable,0)
--	update p
--	set itemValue = n.json
--		from @jsonTable p
--			cross apply (
--				select nodeId, json 
--					from dbo.GetNode(@jsonTable,0)
--				)n
--			where p.objectId = n.nodeId	
--end		
--select dbo.ToJson(itemKey,itemValue) 
--from @jsonTable
--where parentId = 0
--group by parentId


----start with the root
--;WITH 
--JsonOut(
--	[parentId],
--	[objectId],
--	[token],
--	[json]
--)
--AS(
-- -- anchor
--	 SELECT [parentId], [objectId], itemValue, dbo.ToJSON(itemKey, itemValue)
--		FROM @JsonTable
--			where ParentID = 0
--	-- Recursive Call
--	UNION ALL
--		SELECT JsonOut = replace(JsonOut,
--			(select itemValue from @JsonTable where objectId = parentId)
--			,dbo.ToJSON(itemKey, itemValue))
--		FROM @JsonTable
--			where ParentID = ParentID+1
--)
--select JsonOut;
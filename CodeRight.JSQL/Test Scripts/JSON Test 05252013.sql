
--Select Utilities.dbo.ToUnixTime(GetDate())
DECLARE @json nvarchar(max)
set @json = '{"AuthorizationKey":"w7wuKlQ2EftwmkhtDokLqbhnMYm2044kVyl/cEfwB23HcKP+DZsQqg==","Freight.dbo.LoadTask":[{"^Type":"Set","*TaskID":"FFEBC74B-AE9A-4948-8664-00006211E08E","TaskNumber":1,"CarrierDistance":1990},{"^Type":"Set","*TaskID":"F44513AB-4626-46A2-933B-00008EE4145E","TaskNumber":2,"TaskRating":1},{"^Type":"Set","*TaskID":"7C656DA6-643F-4601-B497-0000EFD1467A","TaskNumber":3}]}'
set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","account opened":"/new Date(1369505389647)/","account number":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}}}'
set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","account opened":"/new Date(1369505389647)/","account number":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}},"colors":["blue","green","red","orange"]}'
set @json = '{"_id":"5a5e3bd1-d349-4fdb-af23-02f16e967655","_type":"account information","accountOpened":"/new Date(1369505389647)/","accountNumber":"f959b5c5-48532","_ref":48532,"name":{"prefix":"Dr.","first":"Joe","middle":"Don","Last":"Baker","suffix":"M.D.","fullname":"Dr. Joe Baker, M.D."},"address":{"mailing":{"address1":"5214 La Barranca Drive","address2":"Unit 1208","city":"San Antexico","zipcode":"78148","county":"Bexar","state":"Texas","country":"Texas"},"billing":{"address1":"P.O. Box 50210","city":"Boerne","zipcode":"78151","county":"Bexar","state":"Texas","country":"Texas"}},"contact":{"e-mail":"butt-nugget@buttmonkey.org","telephone":{"mobile":"512-555-1212","pager":"512-553-2215","private":"512-550-0015"}},"colors":["blue","green","red","orange"],"orders":[{"orderNo":"136950538-1526","orderDate":"/new Date(1369505389950)/","salesRep":"A-1526","orderCenter":"5254","orderDetail":[{"itemNo":"A1221-J13","itemDescription":"50 count .44 magnum hp","itemQuantity":12,"unitCost":18.25,"lineItemTotal":219},{"itemNo":"A1221-J10","itemDescription":"50 count .45 mm hp","itemQuantity":12,"unitCost":21.25,"lineItemTotal":255}]},{"orderNo":"136950530-1526","orderDate":"/new Date(136950530500)/","salesRep":"A-1526","orderCenter":"5254","orderDetail":[{"itemNo":"A1221-J13","itemDescription":"50 count .44 magnum hp","itemQuantity":12,"unitCost":18.25,"lineItemTotal":219},{"itemNo":"A1221-J10","itemDescription":"50 count .45 mm hp","itemQuantity":12,"unitCost":21.25,"lineItemTotal":255}]}]}'
SELECT * from Utilities.dbo.rxJsonParse(@json)

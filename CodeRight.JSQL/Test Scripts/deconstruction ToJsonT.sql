
DECLARE @Hierarchy JSONHierarchy
delete @Hierarchy
INSERT INTO @Hierarchy 
select * from parseJSON('{"menu": {
  "id": "file",
  "value": "File",
  "popup": {
    "menuitem": [
      {"value": "New", "onclick": "CreateNewDoc()"},
      {"value": "Open", "onclick": "OpenDoc()"},
      {"value": "Close", "onclick": "CloseDoc()"}
    ]
  }
}}')
  DECLARE
    @JSON NVARCHAR(MAX),
    @NewJSON NVARCHAR(MAX),
    @Where INT,
    @notNumber INT,
    @CrLf CHAR(2)--just a simple utility to save typing!
      
  --firstly get the root token into place 
  SELECT @CrLf=CHAR(13)+CHAR(10),--just CHAR(10) in UNIX
         @JSON = CASE ValueType WHEN 'array' THEN '[' ELSE '{' END
            +@CrLf+ '@Object'+CONVERT(VARCHAR(5),OBJECT_ID)
            +@CrLf+CASE ValueType WHEN 'array' THEN ']' ELSE '}' END            
  FROM @Hierarchy 
    WHERE parent_id IS NULL AND valueType IN ('object','array') --get the root element
/* now we simply iterat from the root token growing each branch and leaf in each iteration. This won't be enormously quick, but it is simple to do. All values, or name/value pairs withing a structure can be created in one SQL Statement*/
  WHILE 1=1
    begin
    SELECT @where= PATINDEX('%[^[a-zA-Z0-9]@Object%',@json)--find NEXT token
    if @where=0 BREAK
    /* this is slightly painful. we get the indent of the object we've found by looking backwards up the string */ 
    SET @NotNumber= PATINDEX('%[^0-9]%', RIGHT(@json,LEN(@JSON+'|')-@Where-8)+' ')--find NEXT token
    SET @NewJSON=NULL --this contains the structure in its JSON form
    SELECT @NewJSON=COALESCE(@NewJSON+',','')
      +COALESCE('"'+NAME+'":','')
      +CASE valuetype 
        WHEN 'array' THEN '['+SPACE(2)
           +'@Object'+CONVERT(VARCHAR(5),OBJECT_ID)+SPACE(2)+']' 
        WHEN 'object' then '{'+SPACE(2)
           +'@Object'+CONVERT(VARCHAR(5),OBJECT_ID)+SPACE(2)+'}'
        WHEN 'string' THEN '"'+dbo.JSONEscaped(StringValue)+'"'
        ELSE StringValue
       END 
     FROM @Hierarchy WHERE parent_id= SUBSTRING(@JSON,@where+8, @Notnumber-1)
     /* basically, we just lookup the structure based on the ID that is appended to the @Object token. Simple eh? */
    --now we replace the token with the structure, maybe with more tokens in it.
    Select @JSON=STUFF (@JSON, @where+1, 8+@NotNumber-1, @NewJSON)
    end
  select @json

--STUFF ( OriginalValue , start , length ,NewValue )

delete @Hierarchy
INSERT INTO @Hierarchy 
Select * from parseJSON('{ 
  "Person": 
  {
     "firstName": "John",
     "lastName": "Smith",
     "age": 25,
     "Address": 
     {
        "streetAddress":"21 2nd Street",
        "city":"New York",
        "state":"NY",
        "postalCode":"10021"
     },
     "PhoneNumbers": 
     {
        "home":"212 555-1234",
        "fax":"646 555-4567"
     }
  }
}
')

SELECT dbo.ToJSONT(@Hierarchy)

delete @Hierarchy
INSERT INTO @Hierarchy 
select * from parseJSON('[ 100, 500, 300, 200, 400 ]')
SELECT dbo.ToJSONT(@Hierarchy)

delete @Hierarchy
INSERT INTO @Hierarchy 
SELECT * from parseJSON('{"widget": {
    "debug": "on",
    "window": {
        "title": "Sample Konfabulator Widget",
        "name": "main_window",
        "width": 500,
        "height": 500
    },
    "image": { 
        "src": "Images/Sun.png",
        "name": "sun1",
        "hOffset": 250,
        "vOffset": 250,
        "alignment": "center"
    },
    "text": {
        "data": "Click Here",
        "size": 36,
        "style": "bold",
        "name": "text1",
        "hOffset": 250,
        "vOffset": 100,
        "alignment": "center",
        "onMouseUp": "sun1.opacity = (sun1.opacity / 100) * 90;"
    }
}}')
SELECT dbo.ToJSONT(@Hierarchy)

INSERT INTO @Hierarchy 
select * from parseJSON('{"menu": {
  "id": "file",
  "value": "File",
  "popup": {
    "menuitem": [
      {"value": "New", "onclick": "CreateNewDoc()"},
      {"value": "Open", "onclick": "OpenDoc()"},
      {"value": "Close", "onclick": "CloseDoc()"}
    ]
  }
}}')


delete @Hierarchy
INSERT INTO @Hierarchy 
select * from parseJSON('
{
    "glossary": {
        "title": "example glossary",
		"GlossDiv": {
            "title": "S",
			"GlossList": {
                "GlossEntry": {
                    "ID": "SGML",
					"SortAs": "SGML",
					"GlossTerm": "Standard Generalized Markup Language",
					"Acronym": "SGML",
					"Abbrev": "ISO 8879:1986",
					"GlossDef": {
                        "para": "A meta-markup language, used to create markup languages such as DocBook.",
						"GlossSeeAlso": ["GML", "XML"]
                    },
					"GlossSee": "markup"
                }
            }
        }
    }
}
')
